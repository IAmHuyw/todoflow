namespace Application.Common;

public record ApiResponse<T>(
    bool Success,
    T? Data,
    string? Message = null,
    IReadOnlyDictionary<string, string[]>? Errors = null)
{
    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new(true, data, message);

    public static ApiResponse<T> Fail(
        string message,
        IReadOnlyDictionary<string, string[]>? errors = null) =>
        new(false, default, message, errors);
}
