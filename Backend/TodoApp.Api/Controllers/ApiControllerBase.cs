using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Application.Common;

namespace TodoApp.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected Guid CurrentUserId
    {
        get
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var userId)
                ? userId
                : throw new AppException("Phiên đăng nhập không hợp lệ.", 401);
        }
    }

    protected ActionResult<ApiResponse<T>> OkResponse<T>(T data, string? message = null) =>
        Ok(ApiResponse<T>.Ok(data, message));

    protected ActionResult<ApiResponse<object>> OkMessage(string message) =>
        Ok(ApiResponse<object>.Ok(new { }, message));
}
