using TodoApp.Application.DTOs;
using TodoApp.Domain.Entities;

namespace TodoApp.Application.Services;

internal static class DtoMapper
{
    public static UserDto ToDto(User user) =>
        new(user.Id, user.Username, user.Email, user.CreatedAt);

    public static CategoryDto ToDto(Category category) =>
        new(category.Id, category.UserId, category.Name, category.Color);

    public static TagDto ToDto(Tag tag) =>
        new(tag.Id, tag.UserId, tag.Name);

    public static SubTaskDto ToDto(SubTask subTask) =>
        new(subTask.Id, subTask.TaskId, subTask.Title, subTask.IsCompleted);

    public static TaskDto ToDto(TodoTask task) =>
        new(
            task.Id,
            task.UserId,
            task.CategoryId,
            task.Title,
            task.Description ?? string.Empty,
            task.Priority,
            task.Status,
            task.DueDate,
            task.IsDeleted,
            task.TaskTags.Select(taskTag => taskTag.TagId).ToArray(),
            task.SubTasks.Select(ToDto).ToArray(),
            task.CreatedAt,
            task.UpdatedAt);

    public static TaskShareDto ToDto(TaskShare share, bool includeTask = false) =>
        new(
            share.Id,
            share.TaskId,
            share.OwnerId,
            share.SharedWithUserId,
            share.Permission,
            share.Status,
            share.CreatedAt,
            share.Owner?.Username,
            share.Owner?.Email,
            share.SharedWithUser?.Username,
            share.SharedWithUser?.Email,
            includeTask && share.Task is not null ? ToDto(share.Task) : null);

    public static NotificationDto ToDto(Notification notification) =>
        new(
            notification.Id,
            notification.UserId,
            notification.TaskId,
            notification.Type,
            notification.Message,
            notification.IsRead,
            notification.CreatedAt);

    public static TaskReminderDto ToDto(TaskReminder reminder) =>
        new(
            reminder.Id,
            reminder.TaskId,
            reminder.RemindAt,
            reminder.Channel,
            reminder.IsSent,
            reminder.CreatedAt);
}
