using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Common;
using Application.Interfaces;

namespace Api.Controllers;

[Authorize]
[Route("api/reminders")]
public class RemindersController : ApiControllerBase
{
    private readonly IReminderService _reminderService;

    public RemindersController(IReminderService reminderService)
    {
        _reminderService = reminderService;
    }

    // Deletes a reminder for a task the current user can edit.
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _reminderService.DeleteAsync(CurrentUserId, id, cancellationToken);
        return OkMessage("Đã xoá nhắc nhở.");
    }
}
