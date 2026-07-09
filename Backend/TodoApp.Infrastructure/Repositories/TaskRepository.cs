using Microsoft.EntityFrameworkCore;
using TodoApp.Application.Interfaces;
using TodoApp.Domain.Entities;
using TodoApp.Domain.Enums;
using TodoApp.Infrastructure.Data;

namespace TodoApp.Infrastructure.Repositories;

public class TaskRepository : Repository<TodoTask>, ITaskRepository
{
    public TaskRepository(AppDbContext context)
        : base(context)
    {
    }

    public IQueryable<TodoTask> QueryForUser(Guid userId, bool includeDetails = false)
    {
        var query = DbSet.Where(task => task.UserId == userId);
        return includeDetails ? IncludeDetails(query) : query;
    }

    public IQueryable<TodoTask> QueryAccessibleForUser(Guid userId, bool includeDetails = false)
    {
        var query = DbSet.Where(task =>
            task.UserId == userId ||
            task.Shares.Any(share =>
                share.SharedWithUserId == userId &&
                share.Status == ShareStatus.Accepted));

        return includeDetails ? IncludeDetails(query) : query;
    }

    public async Task<TodoTask?> GetForUserAsync(
        Guid userId,
        Guid taskId,
        bool includeDetails = false,
        CancellationToken cancellationToken = default)
    {
        var query = QueryForUser(userId, includeDetails);
        return await query.FirstOrDefaultAsync(task => task.Id == taskId, cancellationToken);
    }

    public async Task<TodoTask?> GetAccessibleForUserAsync(
        Guid userId,
        Guid taskId,
        bool includeDetails = false,
        CancellationToken cancellationToken = default)
    {
        var query = QueryAccessibleForUser(userId, includeDetails);
        return await query.FirstOrDefaultAsync(task => task.Id == taskId, cancellationToken);
    }

    private static IQueryable<TodoTask> IncludeDetails(IQueryable<TodoTask> query) =>
        query
            .AsSplitQuery()
            .Include(task => task.SubTasks)
            .Include(task => task.TaskTags)
            .Include(task => task.Shares);
}
