using Application.DTOs;
using Application.Interfaces;
using Domain.Enums;

namespace Application.Services;

public class NoopRealtimeNotifier : IRealtimeNotifier
{
    public Task TaskUpdatedAsync(Guid taskId, TaskDto task, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task TaskStatusChangedAsync(Guid taskId, TodoStatus status, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task SubTaskUpdatedAsync(Guid taskId, SubTaskDto subTask, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task TaskDeletedAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task TaskSharedAsync(TaskShareDto share, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task ShareRespondedAsync(TaskShareDto share, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotificationReceivedAsync(NotificationDto notification, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
