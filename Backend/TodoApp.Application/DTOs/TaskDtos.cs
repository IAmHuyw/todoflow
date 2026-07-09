using TodoApp.Domain.Enums;

namespace TodoApp.Application.DTOs;

public record TaskDto(
    Guid Id,
    Guid UserId,
    Guid? CategoryId,
    string Title,
    string Description,
    Priority Priority,
    TodoStatus Status,
    DateTime? DueDate,
    bool IsDeleted,
    IReadOnlyList<Guid> TagIds,
    IReadOnlyList<SubTaskDto> SubTasks,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public class CreateTaskRequest
{
    public Guid? CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Priority Priority { get; set; } = Priority.Medium;
    public TodoStatus Status { get; set; } = TodoStatus.Todo;
    public DateTime? DueDate { get; set; }
    public List<Guid> TagIds { get; set; } = [];
}

public class UpdateTaskRequest
{
    public Guid? CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Priority Priority { get; set; } = Priority.Medium;
    public TodoStatus Status { get; set; } = TodoStatus.Todo;
    public DateTime? DueDate { get; set; }
    public List<Guid> TagIds { get; set; } = [];
}

public class UpdateTaskStatusRequest
{
    public TodoStatus Status { get; set; }
}

public class TaskQueryParameters
{
    public Guid? CategoryId { get; set; }
    public string? Priority { get; set; }
    public string? Status { get; set; }
    public string? Search { get; set; }
    public string SortBy { get; set; } = "createdAt";
    public string SortDir { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
