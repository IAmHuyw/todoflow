using System.Text.Json;
using Application.Common;

namespace Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            await WriteResponseAsync(context, ex.StatusCode, ApiResponse<object>.Fail(ex.Message, ex.Errors));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled API exception.");
            await WriteResponseAsync(
                context,
                StatusCodes.Status500InternalServerError,
                ApiResponse<object>.Fail("Đã có lỗi hệ thống. Vui lòng thử lại sau."));
        }
    }

    private static async Task WriteResponseAsync(HttpContext context, int statusCode, ApiResponse<object> response)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
