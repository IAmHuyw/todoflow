using Microsoft.EntityFrameworkCore;
using TodoApp.Application.Interfaces;
using TodoApp.Domain.Entities;
using TodoApp.Domain.Enums;
using TodoApp.Infrastructure.Data;

namespace TodoApp.Infrastructure.Seeding;

public class DatabaseSeeder
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public DatabaseSeeder(AppDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var demo = await EnsureUserAsync("demo", "demo@todo.app", "demo1234", cancellationToken);
        await EnsureUserAsync("alex", "alex@todo.app", "alex1234", cancellationToken);

        if (await _context.Tasks.AnyAsync(task => task.UserId == demo.Id, cancellationToken))
        {
            return;
        }

        var work = new Category { UserId = demo.Id, Name = "Công việc", Color = "#3b82f6" };
        var personal = new Category { UserId = demo.Id, Name = "Cá nhân", Color = "#10b981" };
        var learning = new Category { UserId = demo.Id, Name = "Học tập", Color = "#f59e0b" };
        var urgent = new Tag { UserId = demo.Id, Name = "urgent" };
        var idea = new Tag { UserId = demo.Id, Name = "idea" };

        _context.Categories.AddRange(work, personal, learning);
        _context.Tags.AddRange(urgent, idea);

        var now = DateTime.UtcNow;
        var tasks = new[]
        {
            new TodoTask
            {
                UserId = demo.Id,
                Category = work,
                Title = "Hoàn thiện project TodoList",
                Description = "Build full-stack TodoList app cho portfolio.",
                Priority = Priority.High,
                Status = TodoStatus.InProgress,
                DueDate = now.AddDays(2),
                CreatedAt = now,
                UpdatedAt = now,
                TaskTags = new List<TaskTag> { new() { Tag = urgent } },
                SubTasks = new List<SubTask>
                {
                    new() { Title = "Thiết kế database", IsCompleted = true },
                    new() { Title = "Backend API auth", IsCompleted = true },
                    new() { Title = "Frontend dashboard", IsCompleted = false },
                    new() { Title = "Deploy demo", IsCompleted = false }
                }
            },
            new TodoTask
            {
                UserId = demo.Id,
                Category = learning,
                Title = "Ôn EF Core & migration",
                Description = "Đọc lại docs EF Core, luyện migration + query.",
                Priority = Priority.Medium,
                Status = TodoStatus.Todo,
                DueDate = now.AddDays(5),
                CreatedAt = now,
                UpdatedAt = now,
                SubTasks = new List<SubTask>
                {
                    new() { Title = "Đọc chương Migrations", IsCompleted = false }
                }
            },
            new TodoTask
            {
                UserId = demo.Id,
                Category = personal,
                Title = "Đi tập gym 3 buổi/tuần",
                Description = null,
                Priority = Priority.Low,
                Status = TodoStatus.Todo,
                CreatedAt = now,
                UpdatedAt = now
            },
            new TodoTask
            {
                UserId = demo.Id,
                Category = work,
                Title = "Viết README + screenshot",
                Description = "Chuẩn bị bản demo cho CV.",
                Priority = Priority.Medium,
                Status = TodoStatus.Done,
                DueDate = now.AddDays(-1),
                CreatedAt = now,
                UpdatedAt = now,
                TaskTags = new List<TaskTag> { new() { Tag = idea } }
            }
        };

        _context.Tasks.AddRange(tasks);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> EnsureUserAsync(
        string username,
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
        if (user is not null)
        {
            return user;
        }

        user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = _passwordHasher.HashPassword(password),
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }
}
