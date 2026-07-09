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
    RecurrenceType RecurrenceType,
    int RecurrenceInterval,
    DateTime? RecurrenceEndDate,
    Guid? RecurrenceParentId,
    int SortOrder,
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
    public RecurrenceType RecurrenceType { get; set; } = RecurrenceType.None;
    public int RecurrenceInterval { get; set; } = 1;
    public DateTime? RecurrenceEndDate { get; set; }
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
    public RecurrenceType RecurrenceType { get; set; } = RecurrenceType.None;
    public int RecurrenceInterval { get; set; } = 1;
    public DateTime? RecurrenceEndDate { get; set; }
    public List<Guid> TagIds { get; set; } = [];
}

public class UpdateTaskStatusRequest
{
    public TodoStatus Status { get; set; }
}

public record TaskOrderItem(Guid Id, TodoStatus Status, int SortOrder);

public class ReorderTasksRequest
{
    public List<TaskOrderItem> Items { get; set; } = [];
}

public class TaskQueryParameters
{
    public Guid? CategoryId { get; set; }
    public string? Priority { get; set; }
    public string? Status { get; set; }
    public string? Search { get; set; }
    public string SortBy { get; set; } = "sortOrder";
    public string SortDir { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
