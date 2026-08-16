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
    Task CommentAddedAsync(Guid taskId, TaskCommentDto comment, CancellationToken cancellationToken = default);
    Task CommentUpdatedAsync(Guid taskId, TaskCommentDto comment, CancellationToken cancellationToken = default);
    Task CommentDeletedAsync(Guid taskId, Guid commentId, CancellationToken cancellationToken = default);
    Task AssigneeChangedAsync(Guid taskId, TaskDto task, CancellationToken cancellationToken = default);
    Task ActivityAddedAsync(Guid taskId, TaskActivityDto activity, CancellationToken cancellationToken = default);
}
