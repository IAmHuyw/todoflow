using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using Application.Validators;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Auth;
using Infrastructure.Data;
using Infrastructure.UnitOfWork;

namespace Tests;

public class ServiceTests
{
    [Fact]
    public async Task Auth_register_login_refresh_and_logout_flow()
    {
        await using var dbContext = CreateDbContext();
        var auth = CreateAuthService(dbContext);

        var registered = await auth.RegisterAsync(new RegisterRequest
        {
            Username = "testuser",
            Email = "testuser@todo.app",
            Password = "test1234"
        });

        Assert.Equal("testuser", registered.User.Username);
        await Assert.ThrowsAsync<Application.Common.AppException>(() =>
            auth.RegisterAsync(new RegisterRequest
            {
                Username = "testuser",
                Email = "other@todo.app",
                Password = "test1234"
            }));

        var loggedIn = await auth.LoginAsync(new LoginRequest
        {
            EmailOrUsername = "testuser",
            Password = "test1234"
        });

        Assert.False(string.IsNullOrWhiteSpace(loggedIn.AccessToken));

        var refreshed = await auth.RefreshTokenAsync(new RefreshTokenRequest
        {
            RefreshToken = loggedIn.RefreshToken
        });

        Assert.NotEqual(loggedIn.RefreshToken, refreshed.RefreshToken);

        await Assert.ThrowsAsync<Application.Common.AppException>(() =>
            auth.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = loggedIn.RefreshToken }));

        await auth.LogoutAsync(new LogoutRequest { RefreshToken = refreshed.RefreshToken });

        await Assert.ThrowsAsync<Application.Common.AppException>(() =>
            auth.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = refreshed.RefreshToken }));
    }

    [Fact]
    public async Task Task_service_creates_updates_filters_and_soft_deletes_tasks()
    {
        await using var dbContext = CreateDbContext();
        var userId = await SeedUserAsync(dbContext);
        var category = await SeedCategoryAsync(dbContext, userId, "Work");
        var urgentTag = await SeedTagAsync(dbContext, userId, "urgent");
        await SeedTagAsync(dbContext, userId, "idea");
        var taskService = CreateTaskService(dbContext);

        var created = await taskService.CreateAsync(userId, new CreateTaskRequest
        {
            CategoryId = category.Id,
            Title = "Write README",
            Description = "Prepare release notes",
            Priority = Priority.High,
            Status = TodoStatus.Todo,
            DueDate = DateTime.UtcNow.AddDays(2),
            TagIds = [urgentTag.Id]
        });

        Assert.Equal("Write README", created.Title);
        Assert.Equal([urgentTag.Id], created.TagIds);

        var updated = await taskService.UpdateStatusAsync(userId, created.Id, new UpdateTaskStatusRequest
        {
            Status = TodoStatus.InProgress
        });

        Assert.Equal(TodoStatus.InProgress, updated.Status);

        var filtered = await taskService.GetAllAsync(userId, new TaskQueryParameters
        {
            Search = "readme",
            Priority = "high",
            Status = "in_progress",
            SortBy = "dueDate",
            SortDir = "asc",
            Page = 1,
            PageSize = 10
        });

        Assert.Single(filtered.Items);
        Assert.Equal(1, filtered.TotalCount);

        await taskService.DeleteAsync(userId, created.Id);

        var afterDelete = await taskService.GetAllAsync(userId, new TaskQueryParameters());
        Assert.Empty(afterDelete.Items);
        Assert.True(await dbContext.Tasks.IgnoreQueryFilters().AnyAsync(task => task.Id == created.Id && task.IsDeleted));
    }

    [Fact]
    public async Task Category_delete_nulls_category_on_existing_tasks()
    {
        await using var dbContext = CreateDbContext();
        var userId = await SeedUserAsync(dbContext);
        var categoryService = CreateCategoryService(dbContext);
        var taskService = CreateTaskService(dbContext);

        var category = await categoryService.CreateAsync(userId, new CreateCategoryRequest
        {
            Name = "Learning",
            Color = "#f59e0b"
        });

        var task = await taskService.CreateAsync(userId, new CreateTaskRequest
        {
            CategoryId = category.Id,
            Title = "Study EF",
            Priority = Priority.Medium,
            Status = TodoStatus.Todo
        });

        await categoryService.DeleteAsync(userId, category.Id);

        var loaded = await taskService.GetByIdAsync(userId, task.Id);
        Assert.Null(loaded.CategoryId);
    }

    [Fact]
    public async Task Subtask_service_creates_updates_and_deletes_subtasks_for_owner()
    {
        await using var dbContext = CreateDbContext();
        var userId = await SeedUserAsync(dbContext);
        var taskService = CreateTaskService(dbContext);
        var subTaskService = CreateSubTaskService(dbContext);
        var task = await taskService.CreateAsync(userId, new CreateTaskRequest
        {
            Title = "Build API",
            Priority = Priority.High,
            Status = TodoStatus.Todo
        });

        var subTask = await subTaskService.CreateAsync(userId, task.Id, new CreateSubTaskRequest
        {
            Title = "Add controllers"
        });

        var updated = await subTaskService.UpdateAsync(userId, subTask.Id, new UpdateSubTaskRequest
        {
            Title = "Add controllers and middleware",
            IsCompleted = true
        });

        Assert.True(updated.IsCompleted);
        Assert.Equal("Add controllers and middleware", updated.Title);

        await subTaskService.DeleteAsync(userId, subTask.Id);

        Assert.False(await dbContext.SubTasks.AnyAsync(item => item.Id == subTask.Id));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static AuthService CreateAuthService(AppDbContext dbContext) =>
        new(
            new UnitOfWork(dbContext),
            new BCryptPasswordHasher(),
            new FakeTokenService(),
            new NoopEmailSender(),
            new RegisterRequestValidator(),
            new LoginRequestValidator(),
            new RefreshTokenRequestValidator(),
            new LogoutRequestValidator(),
            new ForgotPasswordRequestValidator(),
            new ResetPasswordRequestValidator());

    private static CategoryService CreateCategoryService(AppDbContext dbContext) =>
        new(new UnitOfWork(dbContext), new CreateCategoryRequestValidator(), new UpdateCategoryRequestValidator());

    private static TaskService CreateTaskService(AppDbContext dbContext) =>
        new(
            new UnitOfWork(dbContext),
            new CreateTaskRequestValidator(),
            new UpdateTaskRequestValidator(),
            new UpdateTaskStatusRequestValidator(),
            new ReorderTasksRequestValidator());

    private static SubTaskService CreateSubTaskService(AppDbContext dbContext) =>
        new(new UnitOfWork(dbContext), new CreateSubTaskRequestValidator(), new UpdateSubTaskRequestValidator());

    private static async Task<Guid> SeedUserAsync(AppDbContext dbContext)
    {
        var user = new User
        {
            Username = "testuser",
            Email = $"{Guid.NewGuid():N}@todo.app",
            PasswordHash = new BCryptPasswordHasher().HashPassword("test1234")
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return user.Id;
    }

    private static async Task<Category> SeedCategoryAsync(AppDbContext dbContext, Guid userId, string name)
    {
        var category = new Category { UserId = userId, Name = name, Color = "#3b82f6" };
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync();
        return category;
    }

    private static async Task<Tag> SeedTagAsync(AppDbContext dbContext, Guid userId, string name)
    {
        var tag = new Tag { UserId = userId, Name = name };
        dbContext.Tags.Add(tag);
        await dbContext.SaveChangesAsync();
        return tag;
    }

    private sealed class FakeTokenService : ITokenService
    {
        private int _refreshCounter;

        public DateTime AccessTokenExpiresAt => DateTime.UtcNow.AddMinutes(30);
        public DateTime RefreshTokenExpiresAt => DateTime.UtcNow.AddDays(7);

        public string GenerateAccessToken(User user) => $"access-{user.Id}";

        public string GenerateRefreshToken()
        {
            _refreshCounter++;
            return $"refresh-token-{_refreshCounter}";
        }

        public string HashRefreshToken(string refreshToken) => $"hash-{refreshToken}";
    }
}
