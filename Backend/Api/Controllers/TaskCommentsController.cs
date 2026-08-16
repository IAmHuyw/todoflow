using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Common;
using Application.DTOs;
using Application.Interfaces;

namespace Api.Controllers;

[Authorize]
[Route("api/task-comments")]
public class TaskCommentsController : ApiControllerBase
{
    private readonly ITaskCommentService _taskCommentService;

    public TaskCommentsController(ITaskCommentService taskCommentService)
    {
        _taskCommentService = taskCommentService;
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<TaskCommentDto>>> Update(
        Guid id,
        UpdateTaskCommentRequest request,
        CancellationToken cancellationToken)
    {
        var comment = await _taskCommentService.UpdateAsync(CurrentUserId, id, request, cancellationToken);
        return OkResponse(comment, "Đã cập nhật bình luận.");
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _taskCommentService.DeleteAsync(CurrentUserId, id, cancellationToken);
        return OkMessage("Đã xóa bình luận.");
    }
}
