using Domain.Enums;

namespace Application.DTOs;

public record TaskShareDto(
    Guid Id,
    Guid TaskId,
    Guid OwnerId,
    Guid SharedWithUserId,
    SharePermission Permission,
    ShareStatus Status,
    DateTime CreatedAt,
    string? OwnerUsername,
    string? OwnerEmail,
    string? SharedWithUsername,
    string? SharedWithEmail,
    TaskDto? Task);

public class ShareTaskRequest
{
    public string EmailOrUsername { get; set; } = string.Empty;
    public SharePermission Permission { get; set; } = SharePermission.View;
}

public class RespondShareRequest
{
    public ShareStatus Status { get; set; }
}

public class ChangeSharePermissionRequest
{
    public SharePermission Permission { get; set; } = SharePermission.View;
}
