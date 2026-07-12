using FluentValidation;
using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public class ReminderService : IReminderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateReminderRequest> _createValidator;
    private readonly INotificationService _notificationService;
    private readonly IEmailSender _emailSender;

    public ReminderService(
        IUnitOfWork unitOfWork,
        IValidator<CreateReminderRequest> createValidator,
        INotificationService notificationService,
        IEmailSender emailSender)
    {
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _notificationService = notificationService;
        _emailSender = emailSender;
    }

    public async Task<IReadOnlyList<TaskReminderDto>> GetForTaskAsync(
        Guid userId,
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        await EnsureTaskAccessibleAsync(userId, taskId, cancellationToken);

        var reminders = _unitOfWork.TaskReminders.Query()
            .Where(reminder => reminder.TaskId == taskId)
            .OrderBy(reminder => reminder.RemindAt)
            .ToArray()
            .Select(DtoMapper.ToDto)
            .ToArray();

        return reminders;
    }

    public async Task<TaskReminderDto> CreateAsync(
        Guid userId,
        Guid taskId,
        CreateReminderRequest request,
        CancellationToken cancellationToken = default)
    {
        await _createValidator.EnsureValidAsync(request, cancellationToken);
        await EnsureTaskEditableAsync(userId, taskId, cancellationToken);

        var reminder = new TaskReminder
        {
            TaskId = taskId,
            RemindAt = request.RemindAt.ToUniversalTime(),
            Channel = request.Channel,
            IsSent = false,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.TaskReminders.AddAsync(reminder, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DtoMapper.ToDto(reminder);
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        var reminder = _unitOfWork.TaskReminders.Query()
            .FirstOrDefault(reminder => reminder.Id == id)
            ?? throw new NotFoundException("Không tìm thấy nhắc nhở.");

        await EnsureTaskEditableAsync(userId, reminder.TaskId, cancellationToken);
        _unitOfWork.TaskReminders.Remove(reminder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> ProcessDueAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var dueReminders = _unitOfWork.TaskReminders.Query()
            .Where(reminder => !reminder.IsSent && reminder.RemindAt <= now)
            .ToArray();

        foreach (var reminder in dueReminders)
        {
            var task = await _unitOfWork.Tasks.GetForUserAsync(
                GetTaskOwnerId(reminder.TaskId),
                reminder.TaskId,
                includeDetails: true,
                cancellationToken);

            if (task is null)
            {
                reminder.IsSent = true;
                continue;
            }

            var message = $"Đến giờ nhắc nhở công việc: {task.Title}";

            if (reminder.Channel is ReminderChannel.Email or ReminderChannel.Both)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(task.UserId, cancellationToken);
                if (user is not null)
                {
                    await _emailSender.SendAsync(
                        user.Email,
                        $"TodoFlow nhắc nhở: {task.Title}",
                        BuildReminderEmail(user.Username, task.Title, task.Description, task.DueDate),
                        cancellationToken);
                }
            }

            reminder.IsSent = true;
            await _notificationService.CreateAsync(
                task.UserId,
                task.Id,
                NotificationType.DueDateReminder,
                message,
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return dueReminders.Length;
    }

    private async Task<TodoTask> EnsureTaskAccessibleAsync(
        Guid userId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        return await _unitOfWork.Tasks.GetAccessibleForUserAsync(
                userId,
                taskId,
                includeDetails: true,
                cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy công việc.");
    }

    private async Task<TodoTask> EnsureTaskEditableAsync(
        Guid userId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var task = await EnsureTaskAccessibleAsync(userId, taskId, cancellationToken);
        if (task.UserId == userId)
        {
            return task;
        }

        var canEdit = task.Shares.Any(share =>
            share.SharedWithUserId == userId &&
            share.Status == ShareStatus.Accepted &&
            share.Permission == SharePermission.Edit);

        if (!canEdit)
        {
            throw new AppException("Bạn không có quyền chỉnh sửa công việc này.", 403);
        }

        return task;
    }

    private Guid GetTaskOwnerId(Guid taskId) =>
        _unitOfWork.Tasks.Query()
            .Where(task => task.Id == taskId)
            .Select(task => task.UserId)
            .FirstOrDefault();

    private static string BuildReminderEmail(
        string username,
        string title,
        string? description,
        DateTime? dueDate)
    {
        var safeTitle = EscapeHtml(title);
        var safeDescription = string.IsNullOrWhiteSpace(description)
            ? "Không có mô tả."
            : EscapeHtml(description);
        var due = dueDate.HasValue
            ? dueDate.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
            : "Chưa đặt hạn làm";

        return $"""
            <div style="font-family:Arial,sans-serif;line-height:1.5;color:#111827">
              <h2>TodoFlow nhắc bạn một công việc</h2>
              <p>Xin chào {EscapeHtml(username)},</p>
              <p>Đến giờ nhắc nhở công việc:</p>
              <div style="border:1px solid #e5e7eb;border-radius:8px;padding:12px;margin:12px 0">
                <h3 style="margin:0 0 8px">{safeTitle}</h3>
                <p style="margin:0 0 8px;color:#4b5563">{safeDescription}</p>
                <p style="margin:0;color:#6b7280">Hạn làm: {due}</p>
              </div>
              <p>Chúc bạn xử lý gọn gàng nhé.</p>
            </div>
            """;
    }

    private static string EscapeHtml(string value) =>
        value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&#39;", StringComparison.Ordinal);
}
