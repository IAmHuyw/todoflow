using FluentValidation;
using TodoApp.Application.Common;
using TodoApp.Application.DTOs;
using TodoApp.Application.Interfaces;
using TodoApp.Domain.Entities;
using TodoApp.Domain.Enums;

namespace TodoApp.Application.Services;

public class SubTaskService : ISubTaskService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateSubTaskRequest> _createValidator;
    private readonly IValidator<UpdateSubTaskRequest> _updateValidator;
    private readonly IRealtimeNotifier _notifier;

    public SubTaskService(
        IUnitOfWork unitOfWork,
        IValidator<CreateSubTaskRequest> createValidator,
        IValidator<UpdateSubTaskRequest> updateValidator,
        IRealtimeNotifier? notifier = null)
    {
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _notifier = notifier ?? new NoopRealtimeNotifier();
    }

    public async Task<SubTaskDto> CreateAsync(
        Guid userId,
        Guid taskId,
        CreateSubTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        await _createValidator.EnsureValidAsync(request, cancellationToken);

        var task = await _unitOfWork.Tasks.GetAccessibleForUserAsync(
                userId,
                taskId,
                includeDetails: true,
                cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy task.");
        EnsureCanEdit(userId, task);

        var subTask = new SubTask
        {
            TaskId = task.Id,
            Title = request.Title.Trim(),
            IsCompleted = false
        };

        await _unitOfWork.SubTasks.AddAsync(subTask, cancellationToken);
        task.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = DtoMapper.ToDto(subTask);
        await _notifier.SubTaskUpdatedAsync(task.Id, dto, cancellationToken);
        return dto;
    }

    public async Task<SubTaskDto> UpdateAsync(
        Guid userId,
        Guid id,
        UpdateSubTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        await _updateValidator.EnsureValidAsync(request, cancellationToken);

        var subTask = _unitOfWork.SubTasks.Query()
            .FirstOrDefault(subTask => subTask.Id == id)
            ?? throw new NotFoundException("Không tìm thấy subtask.");

        var task = await _unitOfWork.Tasks.GetAccessibleForUserAsync(
                userId,
                subTask.TaskId,
                includeDetails: true,
                cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy subtask.");
        EnsureCanEdit(userId, task);

        subTask.Title = request.Title.Trim();
        subTask.IsCompleted = request.IsCompleted;
        task.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var dto = DtoMapper.ToDto(subTask);
        await _notifier.SubTaskUpdatedAsync(task.Id, dto, cancellationToken);
        return dto;
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        var subTask = _unitOfWork.SubTasks.Query()
            .FirstOrDefault(subTask => subTask.Id == id)
            ?? throw new NotFoundException("Không tìm thấy subtask.");

        var task = await _unitOfWork.Tasks.GetAccessibleForUserAsync(
                userId,
                subTask.TaskId,
                includeDetails: true,
                cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy subtask.");
        EnsureCanEdit(userId, task);

        task.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.SubTasks.Remove(subTask);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _notifier.SubTaskUpdatedAsync(task.Id, DtoMapper.ToDto(subTask), cancellationToken);
    }

    private static void EnsureCanEdit(Guid userId, TodoTask task)
    {
        if (task.UserId == userId)
        {
            return;
        }

        var canEdit = task.Shares.Any(share =>
            share.SharedWithUserId == userId &&
            share.Status == ShareStatus.Accepted &&
            share.Permission == SharePermission.Edit);

        if (!canEdit)
        {
            throw new AppException("Bạn không có quyền chỉnh sửa task này.", 403);
        }
    }
}
