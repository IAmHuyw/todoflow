using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Repositories;

namespace Infrastructure.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        Users = new Repository<User>(context);
        RefreshTokens = new Repository<RefreshToken>(context);
        PasswordResetOtps = new Repository<PasswordResetOtp>(context);
        Categories = new Repository<Category>(context);
        Tasks = new TaskRepository(context);
        SubTasks = new Repository<SubTask>(context);
        Tags = new Repository<Tag>(context);
        TaskTags = new Repository<TaskTag>(context);
        TaskShares = new Repository<TaskShare>(context);
        Notifications = new Repository<Notification>(context);
        TaskReminders = new Repository<TaskReminder>(context);
    }

    public IRepository<User> Users { get; }
    public IRepository<RefreshToken> RefreshTokens { get; }
    public IRepository<PasswordResetOtp> PasswordResetOtps { get; }
    public IRepository<Category> Categories { get; }
    public ITaskRepository Tasks { get; }
    public IRepository<SubTask> SubTasks { get; }
    public IRepository<Tag> Tags { get; }
    public IRepository<TaskTag> TaskTags { get; }
    public IRepository<TaskShare> TaskShares { get; }
    public IRepository<Notification> Notifications { get; }
    public IRepository<TaskReminder> TaskReminders { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
