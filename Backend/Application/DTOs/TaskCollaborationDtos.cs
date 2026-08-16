using Domain.Enums;

namespace Application.DTOs;

public record TaskCollaboratorDto(
    Guid UserId,
    string Username,
    string? FullName,
    SharePermission Permission,
    bool IsOwner,
    bool IsAssignee);

public record TaskCommentDto(
    Guid Id,
    Guid TaskId,
    Guid AuthorId,
    string AuthorUsername,
    string? AuthorFullName,
    string Content,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public class CreateTaskCommentRequest
{
    public string Content { get; set; } = string.Empty;
}

public class UpdateTaskCommentRequest
{
    public string Content { get; set; } = string.Empty;
}

public class TaskCommentQueryParameters
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 30;
}

public record TaskActivityDto(
    Guid Id,
    Guid TaskId,
    Guid? ActorUserId,
    string? ActorUsername,
    string? ActorFullName,
    TaskActivityType Type,
    string Message,
    DateTime CreatedAt);

public class TaskActivityQueryParameters
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 30;
}
