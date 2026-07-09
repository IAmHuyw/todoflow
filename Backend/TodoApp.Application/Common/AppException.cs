namespace TodoApp.Application.Common;

public class AppException : Exception
{
    public AppException(
        string message,
        int statusCode = 400,
        IReadOnlyDictionary<string, string[]>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }

    public int StatusCode { get; }
    public IReadOnlyDictionary<string, string[]>? Errors { get; }
}
