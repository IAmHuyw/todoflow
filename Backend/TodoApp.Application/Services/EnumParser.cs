using TodoApp.Domain.Enums;

namespace TodoApp.Application.Services;

internal static class EnumParser
{
    public static Priority? ParsePriority(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Normalize(value) switch
        {
            "low" => Priority.Low,
            "medium" => Priority.Medium,
            "high" => Priority.High,
            _ => throw new Common.AppException("Bộ lọc độ ưu tiên không hợp lệ.", 400)
        };
    }

    public static TodoStatus? ParseStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Normalize(value) switch
        {
            "todo" => TodoStatus.Todo,
            "inprogress" => TodoStatus.InProgress,
            "done" => TodoStatus.Done,
            _ => throw new Common.AppException("Bộ lọc trạng thái không hợp lệ.", 400)
        };
    }

    private static string Normalize(string value) =>
        value.Trim().Replace("_", string.Empty).Replace("-", string.Empty).ToLowerInvariant();
}
