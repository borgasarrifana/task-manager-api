using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Data;
using TaskManager.Api.Models;
using TaskManager.Api.Services;
using Xunit;

namespace TaskManager.Api.Tests.Services
{
    public class TaskServiceTests
    {
        private static async Task SeedProjectWithTasksAsync(
            AppDbContext context, int projectId, int ownerId, bool projectCompleted = false, int taskCount = 3)
        {
            context.Projects.Add(new Project
            {
                Id = projectId,
                Name = "Test Project",
                UserId = ownerId,
                IsCompleted = projectCompleted
            });

            for (int i = 1; i <= taskCount; i++)
            {
                context.Tasks.Add(new TaskItem
                {
                    Id = projectId * 100 + i,
                    Title = $"Task {i}",
                    ProjectId = projectId,
                    DueDate = DateTime.UtcNow.AddDays(i)
                });
            }

            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetTaskByIdAsync_WrongOwnerNonAdmin_ReturnsNull()
        {
            using var context = TestDbContextFactory.Create();
            await SeedProjectWithTasksAsync(context, projectId: 1, ownerId: 2, taskCount: 1);

            var service = new TaskService(context);
            var result = await service.GetTaskByIdAsync(101, userId: 1, isAdmin: false);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetTaskByIdAsync_Admin_CanAccessAnyUsersTask()
        {
            using var context = TestDbContextFactory.Create();
            await SeedProjectWithTasksAsync(context, projectId: 1, ownerId: 2, taskCount: 1);

            var service = new TaskService(context);
            var result = await service.GetTaskByIdAsync(101, userId: 1, isAdmin: true);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task CreateTaskAsync_SetsProjectIdAndConvertsDueDateToUtc()
        {
            using var context = TestDbContextFactory.Create();
            context.Projects.Add(new Project { Id = 1, Name = "Test Project", UserId = 1 });
            await context.SaveChangesAsync();

            var service = new TaskService(context);
            var localDueDate = DateTime.SpecifyKind(new DateTime(2026, 9, 1), DateTimeKind.Unspecified);
            var task = new TaskItem { Title = "New Task", DueDate = localDueDate };

            var created = await service.CreateTaskAsync(task, projectId: 1);

            Assert.Equal(1, created.ProjectId);
            Assert.Equal(DateTimeKind.Utc, created.DueDate!.Value.Kind);
        }

        [Fact]
        public async Task UpdateTaskAsync_ProjectCompleted_ReturnsProjectCompletedAndDoesNotChangeTask()
        {
            using var context = TestDbContextFactory.Create();
            await SeedProjectWithTasksAsync(context, projectId: 1, ownerId: 1, projectCompleted: true, taskCount: 1);

            var service = new TaskService(context);
            var update = new TaskItem { Title = "Changed Title", IsDone = true, Priority = Priority.High };
            var result = await service.UpdateTaskAsync(101, update, userId: 1, isAdmin: false);

            Assert.Equal(TaskOperationResult.ProjectCompleted, result);
            var unchanged = await context.Tasks.FirstAsync(t => t.Id == 101);
            Assert.Equal("Task 1", unchanged.Title);
        }

        [Fact]
        public async Task UpdateTaskAsync_WrongOwner_ReturnsNotFound()
        {
            using var context = TestDbContextFactory.Create();
            await SeedProjectWithTasksAsync(context, projectId: 1, ownerId: 2, taskCount: 1);

            var service = new TaskService(context);
            var update = new TaskItem { Title = "Hijacked" };
            var result = await service.UpdateTaskAsync(101, update, userId: 1, isAdmin: false);

            Assert.Equal(TaskOperationResult.NotFound, result);
        }

        [Fact]
        public async Task DeleteTaskAsync_ProjectCompleted_ReturnsProjectCompletedAndDoesNotDelete()
        {
            using var context = TestDbContextFactory.Create();
            await SeedProjectWithTasksAsync(context, projectId: 1, ownerId: 1, projectCompleted: true, taskCount: 1);

            var service = new TaskService(context);
            var result = await service.DeleteTaskAsync(101, userId: 1, isAdmin: false);

            Assert.Equal(TaskOperationResult.ProjectCompleted, result);
            Assert.Equal(1, await context.Tasks.CountAsync());
        }

        [Fact]
        public async Task GetTasksByProjectIdAsync_ReturnsCorrectPageAndTotalCount()
        {
            using var context = TestDbContextFactory.Create();
            await SeedProjectWithTasksAsync(context, projectId: 1, ownerId: 1, taskCount: 25);

            var service = new TaskService(context);
            var page1 = await service.GetTasksByProjectIdAsync(projectId: 1, page: 1, pageSize: 10);
            var page3 = await service.GetTasksByProjectIdAsync(projectId: 1, page: 3, pageSize: 10);

            Assert.Equal(25, page1.TotalCount);
            Assert.Equal(10, page1.Items.Count);
            Assert.Equal(5, page3.Items.Count); // remainder
            Assert.Equal(3, page1.TotalPages);
        }

        [Fact]
        public async Task GetTasksByProjectIdAsync_PageSizeAboveMax_IsClampedTo100()
        {
            using var context = TestDbContextFactory.Create();
            await SeedProjectWithTasksAsync(context, projectId: 1, ownerId: 1, taskCount: 5);

            var service = new TaskService(context);
            var result = await service.GetTasksByProjectIdAsync(projectId: 1, page: 1, pageSize: 500);

            Assert.Equal(100, result.PageSize);
        }
    }
}