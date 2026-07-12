using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Common;
using Application.DTOs;
using Application.Interfaces;

namespace Api.Controllers;

[Authorize]
[Route("api/[controller]")]
public class SubTasksController : ApiControllerBase
{
    private readonly ISubTaskService _subTaskService;

    public SubTasksController(ISubTaskService subTaskService)
    {
        _subTaskService = subTaskService;
    }

    // Updates a subtask that belongs to a task owned by the current user.
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SubTaskDto>>> Update(
        Guid id,
        UpdateSubTaskRequest request,
        CancellationToken cancellationToken)
    {
        var subTask = await _subTaskService.UpdateAsync(CurrentUserId, id, request, cancellationToken);
        return OkResponse(subTask, "Đã cập nhật việc con.");
    }

    // Deletes a subtask that belongs to a task owned by the current user.
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _subTaskService.DeleteAsync(CurrentUserId, id, cancellationToken);
        return OkMessage("Đã xoá việc con.");
    }
}
