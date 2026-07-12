using Domain.Enums;

namespace Domain.Entities;

public class TodoTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid? CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Priority Priority { get; set; } = Priority.Medium;
    public TodoStatus Status { get; set; } = TodoStatus.Todo;
    public DateTime? DueDate { get; set; }
    public RecurrenceType RecurrenceType { get; set; } = RecurrenceType.None;
    public int RecurrenceInterval { get; set; } = 1;
    public DateTime? RecurrenceEndDate { get; set; }
    public Guid? RecurrenceParentId { get; set; }
    public int SortOrder { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Category? Category { get; set; }
    public ICollection<SubTask> SubTasks { get; set; } = new List<SubTask>();
    public ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();
    public ICollection<TaskShare> Shares { get; set; } = new List<TaskShare>();
    public ICollection<TaskReminder> Reminders { get; set; } = new List<TaskReminder>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
