namespace TodoApp.Domain.Entities;

public class SubTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }

    public TodoTask Task { get; set; } = null!;
}
