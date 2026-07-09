namespace TodoApp.Application.DTOs;

public record SubTaskDto(Guid Id, Guid TaskId, string Title, string Note, bool IsCompleted);

public class CreateSubTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class UpdateSubTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Note { get; set; }
    public bool IsCompleted { get; set; }
}
