using TodoApp.Domain.Enums;

namespace TodoApp.Application.DTOs;

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
