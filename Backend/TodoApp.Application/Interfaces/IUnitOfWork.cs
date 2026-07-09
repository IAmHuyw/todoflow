using TodoApp.Domain.Entities;

namespace TodoApp.Application.Interfaces;

public interface IUnitOfWork
{
    IRepository<User> Users { get; }
    IRepository<RefreshToken> RefreshTokens { get; }
    IRepository<Category> Categories { get; }
    ITaskRepository Tasks { get; }
    IRepository<SubTask> SubTasks { get; }
    IRepository<Tag> Tags { get; }
    IRepository<TaskTag> TaskTags { get; }
    IRepository<TaskShare> TaskShares { get; }
    IRepository<Notification> Notifications { get; }
    IRepository<TaskReminder> TaskReminders { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
