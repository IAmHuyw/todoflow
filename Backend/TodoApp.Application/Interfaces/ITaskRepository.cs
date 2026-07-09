using TodoApp.Domain.Entities;

namespace TodoApp.Application.Interfaces;

public interface ITaskRepository : IRepository<TodoTask>
{
    IQueryable<TodoTask> QueryForUser(Guid userId, bool includeDetails = false);
    IQueryable<TodoTask> QueryAccessibleForUser(Guid userId, bool includeDetails = false);

    Task<TodoTask?> GetForUserAsync(
        Guid userId,
        Guid taskId,
        bool includeDetails = false,
        CancellationToken cancellationToken = default);

    Task<TodoTask?> GetAccessibleForUserAsync(
        Guid userId,
        Guid taskId,
        bool includeDetails = false,
        CancellationToken cancellationToken = default);
}
