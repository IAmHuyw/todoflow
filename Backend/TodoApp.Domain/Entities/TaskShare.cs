using TodoApp.Domain.Enums;

namespace TodoApp.Domain.Entities;

public class TaskShare
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TaskId { get; set; }
    public Guid OwnerId { get; set; }
    public Guid SharedWithUserId { get; set; }
    public SharePermission Permission { get; set; } = SharePermission.View;
    public ShareStatus Status { get; set; } = ShareStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TodoTask Task { get; set; } = null!;
    public User Owner { get; set; } = null!;
    public User SharedWithUser { get; set; } = null!;
}
