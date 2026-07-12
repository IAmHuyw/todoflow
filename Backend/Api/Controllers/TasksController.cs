using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Common;
using Application.DTOs;
using Application.Interfaces;

namespace Api.Controllers;

[Authorize]
[Route("api/[controller]")]
public class TasksController : ApiControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ISubTaskService _subTaskService;
    private readonly ITaskShareService _taskShareService;
    private readonly IReminderService _reminderService;

    public TasksController(
        ITaskService taskService,
        ISubTaskService subTaskService,
        ITaskShareService taskShareService,
        IReminderService reminderService)
    {
        _taskService = taskService;
        _subTaskService = subTaskService;
        _taskShareService = taskShareService;
        _reminderService = reminderService;
    }

    // Lists tasks for the current user with optional filters, sorting and paging.
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<TaskDto>>>> GetAll(
        [FromQuery] TaskQueryParameters query,
        CancellationToken cancellationToken)
    {
        var tasks = await _taskService.GetAllAsync(CurrentUserId, query, cancellationToken);
        return OkResponse(tasks);
    }

    // Loads a single task owned by the current user.
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<TaskDto>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var task = await _taskService.GetByIdAsync(CurrentUserId, id, cancellationToken);
        return OkResponse(task);
    }

    // Lists share invitations and accepted shares received by the current user.
    [HttpGet("shared-with-me")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TaskShareDto>>>> GetSharedWithMe(
        CancellationToken cancellationToken)
    {
        var shares = await _taskShareService.GetSharedWithMeAsync(CurrentUserId, cancellationToken);
        return OkResponse(shares);
    }

    // Creates a task owned by the current user.
    [HttpPost]
    public async Task<ActionResult<ApiResponse<TaskDto>>> Create(
        CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var task = await _taskService.CreateAsync(CurrentUserId, request, cancellationToken);
        return OkResponse(task, "Đã tạo công việc.");
    }

    // Updates a task owned by the current user.
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<TaskDto>>> Update(
        Guid id,
        UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var task = await _taskService.UpdateAsync(CurrentUserId, id, request, cancellationToken);
        return OkResponse(task, "Đã cập nhật công việc.");
    }

    // Soft deletes a task owned by the current user.
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _taskService.DeleteAsync(CurrentUserId, id, cancellationToken);
        return OkMessage("Đã xoá công việc.");
    }

    // Updates only the status field for quick dashboard actions.
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<TaskDto>>> UpdateStatus(
        Guid id,
        UpdateTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        var task = await _taskService.UpdateStatusAsync(CurrentUserId, id, request, cancellationToken);
        return OkResponse(task, "Đã cập nhật trạng thái công việc.");
    }

    [HttpPut("reorder")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TaskDto>>>> Reorder(
        ReorderTasksRequest request,
        CancellationToken cancellationToken)
    {
        var tasks = await _taskService.ReorderAsync(CurrentUserId, request, cancellationToken);
        return OkResponse(tasks, "Đã sắp xếp công việc.");
    }

    // Creates a subtask under a task owned by the current user.
    [HttpPost("{taskId:guid}/subtasks")]
    public async Task<ActionResult<ApiResponse<SubTaskDto>>> CreateSubTask(
        Guid taskId,
        CreateSubTaskRequest request,
        CancellationToken cancellationToken)
    {
        var subTask = await _subTaskService.CreateAsync(CurrentUserId, taskId, request, cancellationToken);
        return OkResponse(subTask, "Đã tạo việc con.");
    }

    // Shares a task owned by the current user with another user.
    [HttpPost("{taskId:guid}/share")]
    public async Task<ActionResult<ApiResponse<TaskShareDto>>> Share(
        Guid taskId,
        ShareTaskRequest request,
        CancellationToken cancellationToken)
    {
        var share = await _taskShareService.ShareAsync(CurrentUserId, taskId, request, cancellationToken);
        return OkResponse(share, "Đã chia sẻ công việc.");
    }

    // Lists users that currently have a share record for a task owned by the current user.
    [HttpGet("{taskId:guid}/shares")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TaskShareDto>>>> GetShares(
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var shares = await _taskShareService.GetSharesForTaskAsync(CurrentUserId, taskId, cancellationToken);
        return OkResponse(shares);
    }

    // Creates a reminder for a task the current user can edit.
    [HttpPost("{taskId:guid}/reminders")]
    public async Task<ActionResult<ApiResponse<TaskReminderDto>>> CreateReminder(
        Guid taskId,
        CreateReminderRequest request,
        CancellationToken cancellationToken)
    {
        var reminder = await _reminderService.CreateAsync(CurrentUserId, taskId, request, cancellationToken);
        return OkResponse(reminder, "Đã tạo nhắc nhở.");
    }

    // Lists reminders for a task the current user can access.
    [HttpGet("{taskId:guid}/reminders")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TaskReminderDto>>>> GetReminders(
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var reminders = await _reminderService.GetForTaskAsync(CurrentUserId, taskId, cancellationToken);
        return OkResponse(reminders);
    }
}
