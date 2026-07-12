using Domain.Enums;

namespace Application.DTOs;

public record TaskReminderDto(
    Guid Id,
    Guid TaskId,
    DateTime RemindAt,
    ReminderChannel Channel,
    bool IsSent,
    DateTime CreatedAt);

public class CreateReminderRequest
{
    public DateTime RemindAt { get; set; }
    public ReminderChannel Channel { get; set; } = ReminderChannel.InApp;
}
