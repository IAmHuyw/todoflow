using FluentValidation;
using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

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
            ?? throw new NotFoundException("Không tìm thấy công việc.");
        EnsureCanEdit(userId, task);

        var subTask = new SubTask
        {
            TaskId = task.Id,
            Title = request.Title.Trim(),
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
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
            ?? throw new NotFoundException("Không tìm thấy việc con.");

        var task = await _unitOfWork.Tasks.GetAccessibleForUserAsync(
                userId,
                subTask.TaskId,
                includeDetails: true,
                cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy việc con.");
        EnsureCanEdit(userId, task);

        subTask.Title = request.Title.Trim();
        subTask.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
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
            ?? throw new NotFoundException("Không tìm thấy việc con.");

        var task = await _unitOfWork.Tasks.GetAccessibleForUserAsync(
                userId,
                subTask.TaskId,
                includeDetails: true,
                cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy việc con.");
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
            throw new AppException("Bạn không có quyền chỉnh sửa công việc này.", 403);
        }
    }
}
