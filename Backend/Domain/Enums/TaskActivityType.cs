namespace Domain.Enums;

public enum TaskActivityType
{
    TaskCreated = 1,
    TaskUpdated = 2,
    StatusChanged = 3,
    AssigneeChanged = 4,
    ShareChanged = 5,
    SubTaskCreated = 6,
    SubTaskUpdated = 7,
    SubTaskDeleted = 8,
    CommentAdded = 9,
    CommentUpdated = 10,
    CommentDeleted = 11
}
