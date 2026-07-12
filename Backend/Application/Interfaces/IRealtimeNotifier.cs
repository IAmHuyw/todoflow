using Application.DTOs;
using Domain.Enums;

namespace Application.Interfaces;

public interface IRealtimeNotifier
{
    Task TaskUpdatedAsync(Guid taskId, TaskDto task, CancellationToken cancellationToken = default);
    Task TaskStatusChangedAsync(Guid taskId, TodoStatus status, CancellationToken cancellationToken = default);
    Task SubTaskUpdatedAsync(Guid taskId, SubTaskDto subTask, CancellationToken cancellationToken = default);
    Task TaskDeletedAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task TaskSharedAsync(TaskShareDto share, CancellationToken cancellationToken = default);
    Task ShareRespondedAsync(TaskShareDto share, CancellationToken cancellationToken = default);
    Task NotificationReceivedAsync(NotificationDto notification, CancellationToken cancellationToken = default);
}
