using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Application.Common;
using TodoApp.Application.DTOs;
using TodoApp.Application.Interfaces;

namespace TodoApp.Api.Controllers;

[Authorize]
[Route("api/task-shares")]
public class TaskSharesController : ApiControllerBase
{
    private readonly ITaskShareService _taskShareService;

    public TaskSharesController(ITaskShareService taskShareService)
    {
        _taskShareService = taskShareService;
    }

    // Accepts or rejects a share invitation addressed to the current user.
    [HttpPut("{id:guid}/respond")]
    public async Task<ActionResult<ApiResponse<TaskShareDto>>> Respond(
        Guid id,
        RespondShareRequest request,
        CancellationToken cancellationToken)
    {
        var share = await _taskShareService.RespondAsync(CurrentUserId, id, request, cancellationToken);
        return OkResponse(share, "Đã lưu phản hồi chia sẻ.");
    }

    // Changes share permission for a task owned by the current user.
    [HttpPut("{id:guid}/permission")]
    public async Task<ActionResult<ApiResponse<TaskShareDto>>> ChangePermission(
        Guid id,
        ChangeSharePermissionRequest request,
        CancellationToken cancellationToken)
    {
        var share = await _taskShareService.ChangePermissionAsync(CurrentUserId, id, request, cancellationToken);
        return OkResponse(share, "Đã cập nhật quyền chia sẻ.");
    }

    // Revokes a share from a task owned by the current user.
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Revoke(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _taskShareService.RevokeAsync(CurrentUserId, id, cancellationToken);
        return OkMessage("Đã thu hồi chia sẻ.");
    }
}
