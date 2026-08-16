using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public class TaskShareService : ITaskShareService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRealtimeNotifier _notifier;
    private readonly INotificationService _notificationService;
    private readonly ITaskActivityService _activityService;

    public TaskShareService(
        IUnitOfWork unitOfWork,
        IRealtimeNotifier notifier,
        INotificationService notificationService,
        ITaskActivityService? activityService = null)
    {
        _unitOfWork = unitOfWork;
        _notifier = notifier;
        _notificationService = notificationService;
        _activityService = activityService ?? new TaskActivityService(unitOfWork);
    }

    public async Task<TaskShareDto> ShareAsync(
        Guid ownerId,
        Guid taskId,
        ShareTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(request.Permission))
        {
            throw new AppException("Quyền chia sẻ không hợp lệ.", 400);
        }

        var task = await _unitOfWork.Tasks.GetForUserAsync(
                ownerId,
                taskId,
                includeDetails: true,
                cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy công việc.");

        var target = await FindTargetUserAsync(request.EmailOrUsername, cancellationToken)
            ?? throw new AppException("Không tìm thấy người dùng để chia sẻ.", 404);

        if (target.Id == ownerId)
        {
            throw new AppException("Bạn không thể chia sẻ công việc cho chính mình.", 400);
        }

        var existing = _unitOfWork.TaskShares.Query()
            .FirstOrDefault(share => share.TaskId == taskId && share.SharedWithUserId == target.Id);

        if (existing is not null)
        {
            existing.Permission = request.Permission;
            existing.Status = ShareStatus.Pending;
            var assigneeCleared = ClearAssigneeIfNeeded(task, existing.SharedWithUserId);
            var activity = await _activityService.RecordAsync(
                task.Id,
                ownerId,
                TaskActivityType.ShareChanged,
                $"đã gửi lại lời mời chia sẻ cho {target.Username}",
                cancellationToken);
            var assigneeActivity = await RecordAutomaticUnassignmentAsync(
                task,
                ownerId,
                assigneeCleared,
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            var existingDto = await ToDtoAsync(existing, includeTask: true, cancellationToken);
            await _notifier.TaskSharedAsync(existingDto, cancellationToken);
            await PublishActivityAsync(activity, cancellationToken);
            await PublishOptionalActivityAsync(assigneeActivity, cancellationToken);
            await PublishAssigneeClearedAsync(task, assigneeCleared, cancellationToken);
            await _notificationService.CreateAsync(
                existing.SharedWithUserId,
                existing.TaskId,
                NotificationType.TaskShared,
                $"Bạn có lời mời chia sẻ công việc: {task.Title}",
                cancellationToken);
            return existingDto;
        }

        var share = new TaskShare
        {
            TaskId = task.Id,
            OwnerId = ownerId,
            SharedWithUserId = target.Id,
            Permission = request.Permission,
            Status = ShareStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            Task = task,
            SharedWithUser = target
        };

        await _unitOfWork.TaskShares.AddAsync(share, cancellationToken);
        var createdActivity = await _activityService.RecordAsync(
            task.Id,
            ownerId,
            TaskActivityType.ShareChanged,
            $"đã mời {target.Username} cộng tác",
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = await GetShareDtoAsync(share.Id, includeTask: true, cancellationToken);
        await _notifier.TaskSharedAsync(dto, cancellationToken);
        await PublishActivityAsync(createdActivity, cancellationToken);
        await _notificationService.CreateAsync(
            share.SharedWithUserId,
            share.TaskId,
            NotificationType.TaskShared,
            $"Bạn có lời mời chia sẻ công việc: {task.Title}",
            cancellationToken);
        return dto;
    }

    public async Task<IReadOnlyList<TaskShareDto>> GetSharesForTaskAsync(
        Guid ownerId,
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        var exists = _unitOfWork.Tasks.QueryForUser(ownerId)
            .Any(task => task.Id == taskId);

        if (!exists)
        {
            throw new NotFoundException("Không tìm thấy công việc.");
        }

        var shares = _unitOfWork.TaskShares.Query()
            .Where(share => share.TaskId == taskId && share.OwnerId == ownerId)
            .OrderByDescending(share => share.CreatedAt)
            .ToArray();

        var result = new List<TaskShareDto>(shares.Length);
        foreach (var share in shares)
        {
            result.Add(await ToDtoAsync(share, includeTask: false, cancellationToken));
        }

        return result;
    }

    public async Task<IReadOnlyList<TaskShareDto>> GetSharedWithMeAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var shares = _unitOfWork.TaskShares.Query()
            .Where(share => share.SharedWithUserId == userId)
            .OrderByDescending(share => share.CreatedAt)
            .ToArray();

        var result = new List<TaskShareDto>(shares.Length);
        foreach (var share in shares)
        {
            result.Add(await ToDtoAsync(share, includeTask: true, cancellationToken));
        }

        return result;
    }

    public async Task<TaskShareDto> RespondAsync(
        Guid userId,
        Guid shareId,
        RespondShareRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Status is not ShareStatus.Accepted and not ShareStatus.Rejected)
        {
            throw new AppException("Phản hồi chia sẻ phải là chấp nhận hoặc từ chối.", 400);
        }

        var share = _unitOfWork.TaskShares.Query()
            .FirstOrDefault(share => share.Id == shareId && share.SharedWithUserId == userId)
            ?? throw new NotFoundException("Không tìm thấy lời mời chia sẻ.");

        var task = await _unitOfWork.Tasks.GetForUserAsync(
                share.OwnerId,
                share.TaskId,
                includeDetails: true,
                cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy công việc.");
        share.Status = request.Status;
        var assigneeCleared = request.Status == ShareStatus.Rejected &&
                              ClearAssigneeIfNeeded(task, share.SharedWithUserId);
        var activity = await _activityService.RecordAsync(
            share.TaskId,
            userId,
            TaskActivityType.ShareChanged,
            request.Status == ShareStatus.Accepted ? "đã chấp nhận lời mời chia sẻ" : "đã từ chối lời mời chia sẻ",
            cancellationToken);
        var assigneeActivity = await RecordAutomaticUnassignmentAsync(
            task,
            userId,
            assigneeCleared,
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = await ToDtoAsync(share, includeTask: true, cancellationToken);
        await _notifier.ShareRespondedAsync(dto, cancellationToken);
        await PublishActivityAsync(activity, cancellationToken);
        await PublishOptionalActivityAsync(assigneeActivity, cancellationToken);
        await PublishAssigneeClearedAsync(task, assigneeCleared, cancellationToken);
        return dto;
    }

    public async Task<TaskShareDto> ChangePermissionAsync(
        Guid ownerId,
        Guid shareId,
        ChangeSharePermissionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(request.Permission))
        {
            throw new AppException("Quyền chia sẻ không hợp lệ.", 400);
        }

        var share = _unitOfWork.TaskShares.Query()
            .FirstOrDefault(share => share.Id == shareId && share.OwnerId == ownerId)
            ?? throw new NotFoundException("Không tìm thấy chia sẻ.");

        var task = await _unitOfWork.Tasks.GetForUserAsync(
                ownerId,
                share.TaskId,
                includeDetails: true,
                cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy công việc.");
        share.Permission = request.Permission;
        var assigneeCleared = request.Permission == SharePermission.View &&
                              ClearAssigneeIfNeeded(task, share.SharedWithUserId);
        var activity = await _activityService.RecordAsync(
            share.TaskId,
            ownerId,
            TaskActivityType.ShareChanged,
            $"đã đổi quyền cộng tác thành {request.Permission}",
            cancellationToken);
        var assigneeActivity = await RecordAutomaticUnassignmentAsync(
            task,
            ownerId,
            assigneeCleared,
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = await ToDtoAsync(share, includeTask: true, cancellationToken);
        await _notifier.TaskSharedAsync(dto, cancellationToken);
        await PublishActivityAsync(activity, cancellationToken);
        await PublishOptionalActivityAsync(assigneeActivity, cancellationToken);
        await PublishAssigneeClearedAsync(task, assigneeCleared, cancellationToken);
        return dto;
    }

    public async Task RevokeAsync(
        Guid ownerId,
        Guid shareId,
        CancellationToken cancellationToken = default)
    {
        var share = _unitOfWork.TaskShares.Query()
            .FirstOrDefault(share => share.Id == shareId && share.OwnerId == ownerId)
            ?? throw new NotFoundException("Không tìm thấy chia sẻ.");

        var task = await _unitOfWork.Tasks.GetForUserAsync(
                ownerId,
                share.TaskId,
                includeDetails: true,
                cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy công việc.");
        var dto = await ToDtoAsync(share, includeTask: true, cancellationToken);
        var assigneeCleared = ClearAssigneeIfNeeded(task, share.SharedWithUserId);
        var activity = await _activityService.RecordAsync(
            share.TaskId,
            ownerId,
            TaskActivityType.ShareChanged,
            "đã thu hồi quyền cộng tác",
            cancellationToken);
        var assigneeActivity = await RecordAutomaticUnassignmentAsync(
            task,
            ownerId,
            assigneeCleared,
            cancellationToken);
        _unitOfWork.TaskShares.Remove(share);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _notifier.ShareRespondedAsync(dto, cancellationToken);
        await PublishActivityAsync(activity, cancellationToken);
        await PublishOptionalActivityAsync(assigneeActivity, cancellationToken);
        await PublishAssigneeClearedAsync(task, assigneeCleared, cancellationToken);
    }

    private async Task<User?> FindTargetUserAsync(
        string emailOrUsername,
        CancellationToken cancellationToken)
    {
        var value = emailOrUsername.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new AppException("Vui lòng nhập email hoặc tên đăng nhập.", 400);
        }

        return await Task.FromResult(_unitOfWork.Users.Query()
            .FirstOrDefault(user => user.Email.ToLower() == value || user.Username.ToLower() == value));
    }

    private async Task<TaskShareDto> GetShareDtoAsync(
        Guid shareId,
        bool includeTask,
        CancellationToken cancellationToken)
    {
        var share = _unitOfWork.TaskShares.Query()
            .First(share => share.Id == shareId);

        return await ToDtoAsync(share, includeTask, cancellationToken);
    }

    private async Task<TaskShareDto> ToDtoAsync(
        TaskShare share,
        bool includeTask,
        CancellationToken cancellationToken)
    {
        var owner = _unitOfWork.Users.Query().FirstOrDefault(user => user.Id == share.OwnerId);
        var sharedWith = _unitOfWork.Users.Query().FirstOrDefault(user => user.Id == share.SharedWithUserId);
        TodoTask? task = null;
        if (includeTask)
        {
            task = await _unitOfWork.Tasks.GetAccessibleForUserAsync(
                    share.SharedWithUserId,
                    share.TaskId,
                    includeDetails: true,
                    cancellationToken)
                ?? await _unitOfWork.Tasks.GetForUserAsync(
                    share.OwnerId,
                    share.TaskId,
                    includeDetails: true,
                    cancellationToken);
        }

        return new TaskShareDto(
            share.Id,
            share.TaskId,
            share.OwnerId,
            share.SharedWithUserId,
            share.Permission,
            share.Status,
            share.CreatedAt,
            owner?.Username,
            owner?.Email,
            sharedWith?.Username,
            sharedWith?.Email,
            task is null ? null : DtoMapper.ToDto(task));
    }

    private static bool ClearAssigneeIfNeeded(TodoTask task, Guid sharedWithUserId)
    {
        if (task.AssigneeId != sharedWithUserId)
        {
            return false;
        }

        task.AssigneeId = null;
        task.Assignee = null;
        task.UpdatedAt = DateTime.UtcNow;
        return true;
    }

    private async Task PublishActivityAsync(TaskActivity activity, CancellationToken cancellationToken)
    {
        await _notifier.ActivityAddedAsync(
            activity.TaskId,
            await _activityService.ToDtoAsync(activity, cancellationToken),
            cancellationToken);
    }

    private Task<TaskActivity?> RecordAutomaticUnassignmentAsync(
        TodoTask task,
        Guid actorUserId,
        bool assigneeCleared,
        CancellationToken cancellationToken)
    {
        if (!assigneeCleared)
        {
            return Task.FromResult<TaskActivity?>(null);
        }

        return RecordAsync();

        async Task<TaskActivity?> RecordAsync() => await _activityService.RecordAsync(
            task.Id,
            actorUserId,
            TaskActivityType.AssigneeChanged,
            "đã tự động bỏ người phụ trách do quyền cộng tác thay đổi",
            cancellationToken);
    }

    private async Task PublishOptionalActivityAsync(
        TaskActivity? activity,
        CancellationToken cancellationToken)
    {
        if (activity is not null)
        {
            await PublishActivityAsync(activity, cancellationToken);
        }
    }

    private async Task PublishAssigneeClearedAsync(
        TodoTask task,
        bool assigneeCleared,
        CancellationToken cancellationToken)
    {
        if (!assigneeCleared)
        {
            return;
        }

        var dto = DtoMapper.ToDto(task);
        await _notifier.AssigneeChangedAsync(task.Id, dto, cancellationToken);
        await _notifier.TaskUpdatedAsync(task.Id, dto, cancellationToken);
    }
}
