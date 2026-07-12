using Domain.Enums;

namespace Application.DTOs;

public record NotificationDto(
    Guid Id,
    Guid UserId,
    Guid? TaskId,
    NotificationType Type,
    string Message,
    bool IsRead,
    DateTime CreatedAt);

public class NotificationQueryParameters
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
