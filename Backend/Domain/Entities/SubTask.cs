namespace Domain.Entities;

public class SubTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Note { get; set; }
    public bool IsCompleted { get; set; }

    public TodoTask Task { get; set; } = null!;
}
