using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TodoApp.Application.Common;
using TodoApp.Application.DTOs;
using TodoApp.Application.Interfaces;

namespace TodoApp.Api.Hubs;

[Authorize]
public class TaskHub : Hub
{
    private readonly ITaskService _taskService;

    public TaskHub(ITaskService taskService)
    {
        _taskService = taskService;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        await base.OnConnectedAsync();
    }

    public async Task JoinTaskGroup(Guid taskId)
    {
        var userId = GetCurrentUserId();
        await _taskService.GetByIdAsync(userId, taskId, Context.ConnectionAborted);
        await Groups.AddToGroupAsync(Context.ConnectionId, TaskGroup(taskId));
    }

    public Task LeaveTaskGroup(Guid taskId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, TaskGroup(taskId));

    public static string TaskGroup(Guid taskId) => $"task-{taskId}";

    public static string UserGroup(Guid userId) => $"user-{userId}";

    private Guid GetCurrentUserId()
    {
        var value = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new AppException("Invalid authenticated user.", 401);
    }
}
