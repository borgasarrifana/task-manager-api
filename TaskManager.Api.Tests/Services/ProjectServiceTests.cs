using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Models;
using TaskManager.Api.Services;
using Xunit;

namespace TaskManager.Api.Tests.Services
{
    public class ProjectServiceTests
    {
        [Fact]
        public async Task GetAllProjectsAsync_NonAdmin_ReturnsOnlyOwnProjects()
        {
            using var context = TestDbContextFactory.Create();
            context.Users.AddRange(
                new User { Id = 1, Username = "alice" },
                new User { Id = 2, Username = "bob" }
            );
            context.Projects.AddRange(
                new Project { Id = 1, Name = "Alice's Project", UserId = 1 },
                new Project { Id = 2, Name = "Bob's Project", UserId = 2 }
            );
            await context.SaveChangesAsync();

            var service = new ProjectService(context);
            var result = await service.GetAllProjectsAsync(userId: 1, isAdmin: false);

            Assert.Single(result.Items);
            Assert.Equal("Alice's Project", result.Items.First().Name);
        }

        [Fact]
        public async Task GetAllProjectsAsync_Admin_ReturnsAllProjects()
        {
            using var context = TestDbContextFactory.Create();
            context.Users.AddRange(
                new User { Id = 1, Username = "alice" },
                new User { Id = 2, Username = "bob" }
            );
            context.Projects.AddRange(
                new Project { Id = 1, Name = "Alice's Project", UserId = 1 },
                new Project { Id = 2, Name = "Bob's Project", UserId = 2 }
            );
            await context.SaveChangesAsync();

            var service = new ProjectService(context);
            var result = await service.GetAllProjectsAsync(userId: 1, isAdmin: true);

            Assert.Equal(2, result.Items.Count);
        }

        [Fact]
        public async Task GetProjectByIdAsync_WrongOwnerNonAdmin_ReturnsNull()
        {
            using var context = TestDbContextFactory.Create();
            context.Projects.Add(new Project { Id = 1, Name = "Bob's Project", UserId = 2 });
            await context.SaveChangesAsync();

            var service = new ProjectService(context);
            var result = await service.GetProjectByIdAsync(1, userId: 1, isAdmin: false);

            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteProjectAsync_WrongOwner_ReturnsFalseAndDoesNotDelete()
        {
            using var context = TestDbContextFactory.Create();
            context.Projects.Add(new Project { Id = 1, Name = "Bob's Project", UserId = 2 });
            await context.SaveChangesAsync();

            var service = new ProjectService(context);
            var success = await service.DeleteProjectAsync(1, userId: 1, isAdmin: false);

            Assert.False(success);
            Assert.Equal(1, await context.Projects.CountAsync());
        }

        [Fact]
        public async Task CompleteProjectAsync_MarksProjectAndAllTasksDone()
        {
            using var context = TestDbContextFactory.Create();
            context.Projects.Add(new Project
            {
                Id = 1,
                Name = "Launch",
                UserId = 1,
                Tasks = new List<TaskItem>
                {
                    new TaskItem { Id = 1, Title = "Task A", IsDone = false },
                    new TaskItem { Id = 2, Title = "Task B", IsDone = false }
                }
            });
            await context.SaveChangesAsync();

            var service = new ProjectService(context);
            var success = await service.CompleteProjectAsync(1, userId: 1, isAdmin: false);

            Assert.True(success);
            var updated = await context.Projects.Include(p => p.Tasks).FirstAsync(p => p.Id == 1);
            Assert.True(updated.IsCompleted);
            Assert.All(updated.Tasks, t => Assert.True(t.IsDone));
        }

        [Fact]
        public async Task ReopenProjectAsync_SetsIsCompletedFalse()
        {
            using var context = TestDbContextFactory.Create();
            context.Projects.Add(new Project { Id = 1, Name = "Launch", UserId = 1, IsCompleted = true });
            await context.SaveChangesAsync();

            var service = new ProjectService(context);
            var success = await service.ReopenProjectAsync(1, userId: 1, isAdmin: false);

            Assert.True(success);
            var updated = await context.Projects.FirstAsync(p => p.Id == 1);
            Assert.False(updated.IsCompleted);
        }
    }
}