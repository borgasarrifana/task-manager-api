using TaskManager.Api.Models;
using TaskManager.Api.Services;
using Xunit;

namespace TaskManager.Api.Tests.Services
{
    public class DashboardServiceTests
    {
        // "Today" is pinned so overdue / due-this-week results are deterministic
        private static readonly DateTimeOffset Now = new(2026, 9, 25, 10, 0, 0, TimeSpan.Zero);

        private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => now;
        }

        private static DateTime Utc(int month, int day) =>
            new(2026, month, day, 0, 0, 0, DateTimeKind.Utc);

        private static DashboardService CreateService(TaskManager.Api.Data.AppDbContext context) =>
            new(context, new FixedTimeProvider(Now));

        // Users must be seeded: the projection reads Project.User, and the InMemory
        // provider treats the required FK as an INNER JOIN
        private static void SeedUsers(TaskManager.Api.Data.AppDbContext context)
        {
            context.Users.AddRange(
                new User { Id = 1, Username = "alice" },
                new User { Id = 2, Username = "bob" }
            );
        }

        [Fact]
        public async Task GetDashboardAsync_NonAdmin_OnlyIncludesOwnProjects()
        {
            using var context = TestDbContextFactory.Create();
            SeedUsers(context);
            context.Projects.AddRange(
                new Project { Id = 1, Name = "Alice's Project", UserId = 1 },
                new Project { Id = 2, Name = "Bob's Project", UserId = 2 }
            );
            await context.SaveChangesAsync();

            var result = await CreateService(context).GetDashboardAsync(userId: 1, isAdmin: false);

            Assert.Equal(1, result.Totals.ActiveProjects);
            Assert.Single(result.Projects);
            Assert.Equal("Alice's Project", result.Projects[0].Name);
        }

        [Fact]
        public async Task GetDashboardAsync_Admin_IncludesAllProjects()
        {
            using var context = TestDbContextFactory.Create();
            SeedUsers(context);
            context.Projects.AddRange(
                new Project { Id = 1, Name = "Alice's Project", UserId = 1 },
                new Project { Id = 2, Name = "Bob's Project", UserId = 2 }
            );
            await context.SaveChangesAsync();

            var result = await CreateService(context).GetDashboardAsync(userId: 1, isAdmin: true);

            Assert.Equal(2, result.Totals.ActiveProjects);
            Assert.Contains(result.Projects, p => p.OwnerUsername == "bob");
        }

        [Fact]
        public async Task GetDashboardAsync_Totals_ExcludeCompletedProjects()
        {
            using var context = TestDbContextFactory.Create();
            SeedUsers(context);
            context.Projects.AddRange(
                new Project
                {
                    Id = 1, Name = "Active", UserId = 1,
                    Tasks = new List<TaskItem>
                    {
                        new() { Title = "A", IsDone = true },
                        new() { Title = "B" },
                        new() { Title = "C" },
                        new() { Title = "D" }
                    }
                },
                new Project
                {
                    Id = 2, Name = "Finished", UserId = 1, IsCompleted = true,
                    Tasks = new List<TaskItem>
                    {
                        new() { Title = "E", IsDone = true },
                        new() { Title = "F", IsDone = true }
                    }
                }
            );
            await context.SaveChangesAsync();

            var result = await CreateService(context).GetDashboardAsync(userId: 1);

            Assert.Equal(1, result.Totals.ActiveProjects);
            Assert.Equal(1, result.Totals.CompletedProjects);
            Assert.Equal(4, result.Totals.TotalTasks);
            Assert.Equal(1, result.Totals.DoneTasks);
            Assert.Equal(3, result.Totals.OpenTasks);
            Assert.Equal(25, result.Totals.CompletionPercent);
            Assert.DoesNotContain(result.Projects, p => p.Name == "Finished");
        }

        [Fact]
        public async Task GetDashboardAsync_CountsOverdueAndDueThisWeek_RelativeToToday()
        {
            using var context = TestDbContextFactory.Create();
            SeedUsers(context);
            context.Projects.Add(new Project
            {
                Id = 1, Name = "Deadlines", UserId = 1,
                Tasks = new List<TaskItem>
                {
                    new() { Title = "Overdue", DueDate = Utc(9, 20) },
                    new() { Title = "Overdue but done", DueDate = Utc(9, 20), IsDone = true },
                    new() { Title = "Due today", DueDate = Utc(9, 25) },
                    new() { Title = "Due in 2 days", DueDate = Utc(9, 27) },
                    new() { Title = "Due next month", DueDate = Utc(10, 10) },
                    new() { Title = "No date" }
                }
            });
            await context.SaveChangesAsync();

            var result = await CreateService(context).GetDashboardAsync(userId: 1);

            Assert.Equal(1, result.Totals.OverdueTasks);
            Assert.Equal(2, result.Totals.DueThisWeek);
            Assert.Equal(1, result.Projects[0].OverdueTasks);
        }

        [Fact]
        public async Task GetDashboardAsync_OpenByPriority_CountsOnlyOpenTasksInActiveProjects()
        {
            using var context = TestDbContextFactory.Create();
            SeedUsers(context);
            context.Projects.AddRange(
                new Project
                {
                    Id = 1, Name = "Active", UserId = 1,
                    Tasks = new List<TaskItem>
                    {
                        new() { Title = "H1", Priority = Priority.High },
                        new() { Title = "H2", Priority = Priority.High },
                        new() { Title = "M1", Priority = Priority.Medium },
                        new() { Title = "H-done", Priority = Priority.High, IsDone = true }
                    }
                },
                new Project
                {
                    Id = 2, Name = "Finished", UserId = 1, IsCompleted = true,
                    Tasks = new List<TaskItem>
                    {
                        new() { Title = "L-done", Priority = Priority.Low, IsDone = true }
                    }
                }
            );
            await context.SaveChangesAsync();

            var result = await CreateService(context).GetDashboardAsync(userId: 1);

            Assert.Equal(2, result.OpenByPriority.High);
            Assert.Equal(1, result.OpenByPriority.Medium);
            Assert.Equal(0, result.OpenByPriority.Low);
        }

        [Fact]
        public async Task GetDashboardAsync_Upcoming_OrderedByDueDate_ExcludesDoneAndUndated()
        {
            using var context = TestDbContextFactory.Create();
            SeedUsers(context);
            context.Projects.Add(new Project
            {
                Id = 1, Name = "Deadlines", UserId = 1,
                Tasks = new List<TaskItem>
                {
                    new() { Title = "Later", DueDate = Utc(10, 1) },
                    new() { Title = "Overdue", DueDate = Utc(9, 20) },
                    new() { Title = "Soon", DueDate = Utc(9, 26) },
                    new() { Title = "Done", DueDate = Utc(9, 21), IsDone = true },
                    new() { Title = "No date" }
                }
            });
            await context.SaveChangesAsync();

            var result = await CreateService(context).GetDashboardAsync(userId: 1);

            Assert.Equal(new[] { "Overdue", "Soon", "Later" }, result.Upcoming.Select(t => t.Title));
            Assert.True(result.Upcoming[0].IsOverdue);
            Assert.False(result.Upcoming[1].IsOverdue);
            Assert.All(result.Upcoming, t => Assert.Equal("Deadlines", t.ProjectName));
        }
    }
}