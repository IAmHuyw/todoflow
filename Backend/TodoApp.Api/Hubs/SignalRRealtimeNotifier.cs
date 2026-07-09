using Microsoft.AspNetCore.SignalR;
using TodoApp.Application.DTOs;
using TodoApp.Application.Interfaces;
using TodoApp.Domain.Enums;

namespace TodoApp.Api.Hubs;

public class SignalRRealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<TaskHub> _hubContext;

    public SignalRRealtimeNotifier(IHubContext<TaskHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task TaskUpdatedAsync(Guid taskId, TaskDto task, CancellationToken cancellationToken = default) =>
        _hubContext.Clients.Group(TaskHub.TaskGroup(taskId))
            .SendAsync("TaskUpdated", task, cancellationToken);

    public Task TaskStatusChangedAsync(Guid taskId, TodoStatus status, CancellationToken cancellationToken = default) =>
        _hubContext.Clients.Group(TaskHub.TaskGroup(taskId))
            .SendAsync("TaskStatusChanged", new { taskId, status }, cancellationToken);

    public Task SubTaskUpdatedAsync(Guid taskId, SubTaskDto subTask, CancellationToken cancellationToken = default) =>
        _hubContext.Clients.Group(TaskHub.TaskGroup(taskId))
            .SendAsync("SubTaskUpdated", subTask, cancellationToken);

    public Task TaskDeletedAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        _hubContext.Clients.Group(TaskHub.TaskGroup(taskId))
            .SendAsync("TaskDeleted", new { taskId }, cancellationToken);

    public async Task TaskSharedAsync(TaskShareDto share, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group(TaskHub.UserGroup(share.SharedWithUserId))
            .SendAsync("TaskShared", share, cancellationToken);
        await _hubContext.Clients.Group(TaskHub.TaskGroup(share.TaskId))
            .SendAsync("TaskShared", share, cancellationToken);
    }

    public async Task ShareRespondedAsync(TaskShareDto share, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group(TaskHub.UserGroup(share.OwnerId))
            .SendAsync("ShareResponded", share, cancellationToken);
        await _hubContext.Clients.Group(TaskHub.UserGroup(share.SharedWithUserId))
            .SendAsync("ShareResponded", share, cancellationToken);
        await _hubContext.Clients.Group(TaskHub.TaskGroup(share.TaskId))
            .SendAsync("ShareResponded", share, cancellationToken);
    }

    public Task NotificationReceivedAsync(NotificationDto notification, CancellationToken cancellationToken = default) =>
        _hubContext.Clients.Group(TaskHub.UserGroup(notification.UserId))
            .SendAsync("NotificationReceived", notification, cancellationToken);
}
