using TodoApp.Domain.Enums;

namespace TodoApp.Domain.Entities;

public class TaskReminder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TaskId { get; set; }
    public DateTime RemindAt { get; set; }
    public ReminderChannel Channel { get; set; } = ReminderChannel.InApp;
    public bool IsSent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TodoTask Task { get; set; } = null!;
}
