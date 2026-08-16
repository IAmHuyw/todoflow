using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public class TaskActivityService : ITaskActivityService
{
    private readonly IUnitOfWork _unitOfWork;

    public TaskActivityService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<TaskActivityDto>> GetAllAsync(
        Guid userId,
        Guid taskId,
        TaskActivityQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        _ = await _unitOfWork.Tasks.GetAccessibleForUserAsync(userId, taskId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy công việc.");

        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var activities = _unitOfWork.TaskActivities.Query()
            .Where(activity => activity.TaskId == taskId)
            .OrderByDescending(activity => activity.CreatedAt);
        var totalCount = activities.Count();
        var pageItems = activities.Skip((page - 1) * pageSize).Take(pageSize).ToArray();
        var actorIds = pageItems.Where(item => item.ActorUserId.HasValue)
            .Select(item => item.ActorUserId!.Value)
            .Distinct()
            .ToArray();
        var actors = _unitOfWork.Users.Query()
            .Where(user => actorIds.Contains(user.Id))
            .ToDictionary(user => user.Id);

        foreach (var activity in pageItems)
        {
            if (activity.ActorUserId.HasValue && actors.TryGetValue(activity.ActorUserId.Value, out var actor))
            {
                activity.ActorUser = actor;
            }
        }

        var items = pageItems.Select(DtoMapper.ToDto).ToArray();
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResult<TaskActivityDto>(items, page, pageSize, totalCount, totalPages);
    }

    public async Task<TaskActivity> RecordAsync(
        Guid taskId,
        Guid? actorUserId,
        TaskActivityType type,
        string message,
        CancellationToken cancellationToken = default)
    {
        var activity = new TaskActivity
        {
            TaskId = taskId,
            ActorUserId = actorUserId,
            Type = type,
            Message = message.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.TaskActivities.AddAsync(activity, cancellationToken);
        return activity;
    }

    public Task<TaskActivityDto> ToDtoAsync(
        TaskActivity activity,
        CancellationToken cancellationToken = default)
    {
        if (activity.ActorUserId.HasValue)
        {
            activity.ActorUser = _unitOfWork.Users.Query()
                .FirstOrDefault(user => user.Id == activity.ActorUserId.Value);
        }

        return Task.FromResult(DtoMapper.ToDto(activity));
    }
}
