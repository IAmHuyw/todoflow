using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Common;
using Application.DTOs;
using Application.Interfaces;

namespace Api.Controllers;

[Authorize]
[Route("api/[controller]")]
public class NotificationsController : ApiControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    // Lists notifications for the current user with paging.
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<NotificationDto>>>> GetAll(
        [FromQuery] NotificationQueryParameters query,
        CancellationToken cancellationToken)
    {
        var notifications = await _notificationService.GetAllAsync(CurrentUserId, query, cancellationToken);
        return OkResponse(notifications);
    }

    // Marks one notification as read.
    [HttpPut("{id:guid}/read")]
    public async Task<ActionResult<ApiResponse<object>>> MarkRead(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _notificationService.MarkReadAsync(CurrentUserId, id, cancellationToken);
        return OkMessage("Đã đánh dấu thông báo là đã đọc.");
    }

    // Marks all notifications as read for the current user.
    [HttpPut("read-all")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAllRead(CancellationToken cancellationToken)
    {
        await _notificationService.MarkAllReadAsync(CurrentUserId, cancellationToken);
        return OkMessage("Đã đánh dấu tất cả thông báo là đã đọc.");
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _notificationService.DeleteAsync(CurrentUserId, id, cancellationToken);
        return OkMessage("Đã xoá thông báo.");
    }

    [HttpDelete]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAll(CancellationToken cancellationToken)
    {
        await _notificationService.DeleteAllAsync(CurrentUserId, cancellationToken);
        return OkMessage("Đã xoá tất cả thông báo.");
    }
}
