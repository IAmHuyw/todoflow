using Domain.Enums;

namespace Domain.Entities;

public class TaskActivity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TaskId { get; set; }
    public Guid? ActorUserId { get; set; }
    public TaskActivityType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TodoTask Task { get; set; } = null!;
    public User? ActorUser { get; set; }
}
