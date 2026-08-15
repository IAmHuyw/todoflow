using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<DashboardSummaryDto> GetSummaryAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var trendStart = today.AddDays(-6);
        var upcomingEnd = tomorrow.AddDays(7);
        var tasks = _unitOfWork.Tasks.QueryForUser(userId);
        var categories = _unitOfWork.Categories.Query()
            .Where(category => category.UserId == userId);

        var totalTaskCount = tasks.Count();
        var todoCount = tasks.Count(task => task.Status == TodoStatus.Todo);
        var inProgressCount = tasks.Count(task => task.Status == TodoStatus.InProgress);
        var doneCount = tasks.Count(task => task.Status == TodoStatus.Done);
        var overdueCount = tasks.Count(task =>
            task.Status != TodoStatus.Done &&
            task.DueDate.HasValue &&
            task.DueDate.Value < today);
        var dueTodayCount = tasks.Count(task =>
            task.Status != TodoStatus.Done &&
            task.DueDate.HasValue &&
            task.DueDate.Value >= today &&
            task.DueDate.Value < tomorrow);

        var todayTasks = ProjectTaskSummaries(
                tasks
                    .Where(task =>
                        task.Status != TodoStatus.Done &&
                        task.DueDate.HasValue &&
                        task.DueDate.Value < tomorrow)
                    .OrderBy(task => task.DueDate)
                    .ThenByDescending(task => task.Priority)
                    .ThenBy(task => task.Title)
                    .Take(5),
                categories)
            .ToArray();

        var upcomingTasks = ProjectTaskSummaries(
                tasks
                    .Where(task =>
                        task.Status != TodoStatus.Done &&
                        task.DueDate.HasValue &&
                        task.DueDate.Value >= tomorrow &&
                        task.DueDate.Value < upcomingEnd)
                    .OrderBy(task => task.DueDate)
                    .ThenByDescending(task => task.Priority)
                    .ThenBy(task => task.Title)
                    .Take(5),
                categories)
            .ToArray();

        var createdCounts = tasks
            .Where(task => task.CreatedAt >= trendStart && task.CreatedAt < tomorrow)
            .GroupBy(task => task.CreatedAt.Date)
            .Select(group => new { Date = group.Key, Count = group.Count() })
            .ToArray()
            .ToDictionary(item => item.Date, item => item.Count);

        var createdTaskTrend = Enumerable.Range(0, 7)
            .Select(offset => trendStart.AddDays(offset))
            .Select(date => new DashboardTrendPointDto(
                DateOnly.FromDateTime(date),
                createdCounts.GetValueOrDefault(date, 0)))
            .ToArray();

        var categorySummaries = (
                from category in categories
                join task in tasks.Where(task => task.CategoryId.HasValue)
                    on category.Id equals task.CategoryId!.Value into categoryTasks
                let total = categoryTasks.Count()
                let open = categoryTasks.Count(task => task.Status != TodoStatus.Done)
                where total > 0
                orderby open descending, total descending, category.Name
                select new DashboardCategorySummaryDto(
                    category.Id,
                    category.Name,
                    category.Color,
                    total,
                    open))
            .Take(5)
            .ToArray();

        return Task.FromResult(new DashboardSummaryDto(
            totalTaskCount,
            todoCount,
            inProgressCount,
            doneCount,
            overdueCount,
            dueTodayCount,
            todayTasks,
            upcomingTasks,
            createdTaskTrend,
            categorySummaries));
    }

    private static IQueryable<DashboardTaskSummaryDto> ProjectTaskSummaries(
        IQueryable<TodoTask> tasks,
        IQueryable<Category> categories) =>
        from task in tasks
        join category in categories
            on task.CategoryId equals (Guid?)category.Id into matchingCategories
        from category in matchingCategories.DefaultIfEmpty()
        select new DashboardTaskSummaryDto(
            task.Id,
            task.Title,
            task.Priority,
            task.Status,
            task.DueDate,
            category == null ? null : category.Name,
            category == null ? null : category.Color);
}
