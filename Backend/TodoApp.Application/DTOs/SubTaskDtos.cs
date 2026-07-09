namespace TodoApp.Application.DTOs;

public record SubTaskDto(Guid Id, Guid TaskId, string Title, bool IsCompleted);

public class CreateSubTaskRequest
{
    public string Title { get; set; } = string.Empty;
}

public class UpdateSubTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}
