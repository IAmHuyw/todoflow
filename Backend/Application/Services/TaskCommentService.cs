using FluentValidation;
using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public class TaskCommentService : ITaskCommentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateTaskCommentRequest> _createValidator;
    private readonly IValidator<UpdateTaskCommentRequest> _updateValidator;
    private readonly ITaskActivityService _activityService;
    private readonly IRealtimeNotifier _notifier;
    private readonly INotificationService _notificationService;

    public TaskCommentService(
        IUnitOfWork unitOfWork,
        IValidator<CreateTaskCommentRequest> createValidator,
        IValidator<UpdateTaskCommentRequest> updateValidator,
        ITaskActivityService activityService,
        IRealtimeNotifier notifier,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _activityService = activityService;
        _notifier = notifier;
        _notificationService = notificationService;
    }

    public async Task<PagedResult<TaskCommentDto>> GetAllAsync(
        Guid userId,
        Guid taskId,
        TaskCommentQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        _ = await GetAccessibleTaskAsync(userId, taskId, cancellationToken);
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var comments = _unitOfWork.TaskComments.Query()
            .Where(comment => comment.TaskId == taskId)
            .OrderByDescending(comment => comment.CreatedAt);
        var totalCount = comments.Count();
        var pageItems = comments.Skip((page - 1) * pageSize).Take(pageSize).ToArray();
        PopulateAuthors(pageItems);
        var items = pageItems.Select(DtoMapper.ToDto).ToArray();
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResult<TaskCommentDto>(items, page, pageSize, totalCount, totalPages);
    }

    public async Task<TaskCommentDto> CreateAsync(
        Guid userId,
        Guid taskId,
        CreateTaskCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        await _createValidator.EnsureValidAsync(request, cancellationToken);
        var task = await GetAccessibleTaskAsync(userId, taskId, cancellationToken);
        var author = _unitOfWork.Users.Query().First(user => user.Id == userId);
        var comment = new TaskComment
        {
            TaskId = taskId,
            AuthorId = userId,
            Author = author,
            Content = request.Content.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.TaskComments.AddAsync(comment, cancellationToken);
        var activity = await _activityService.RecordAsync(
            taskId,
            userId,
            TaskActivityType.CommentAdded,
            "đã thêm một bình luận",
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = DtoMapper.ToDto(comment);
        var activityDto = await _activityService.ToDtoAsync(activity, cancellationToken);
        await _notifier.CommentAddedAsync(taskId, dto, cancellationToken);
        await _notifier.ActivityAddedAsync(taskId, activityDto, cancellationToken);
        await NotifyCollaboratorsAsync(userId, task, author, cancellationToken);
        return dto;
    }

    public async Task<TaskCommentDto> UpdateAsync(
        Guid userId,
        Guid id,
        UpdateTaskCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        await _updateValidator.EnsureValidAsync(request, cancellationToken);
        var comment = _unitOfWork.TaskComments.Query().FirstOrDefault(item => item.Id == id)
            ?? throw new NotFoundException("Không tìm thấy bình luận.");
        _ = await GetAccessibleTaskAsync(userId, comment.TaskId, cancellationToken);
        if (comment.AuthorId != userId)
        {
            throw new AppException("Bạn chỉ có thể sửa bình luận của mình.", 403);
        }

        comment.Content = request.Content.Trim();
        comment.UpdatedAt = DateTime.UtcNow;
        var activity = await _activityService.RecordAsync(
            comment.TaskId,
            userId,
            TaskActivityType.CommentUpdated,
            "đã sửa một bình luận",
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        PopulateAuthors([comment]);

        var dto = DtoMapper.ToDto(comment);
        await _notifier.CommentUpdatedAsync(comment.TaskId, dto, cancellationToken);
        await _notifier.ActivityAddedAsync(
            comment.TaskId,
            await _activityService.ToDtoAsync(activity, cancellationToken),
            cancellationToken);
        return dto;
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        var comment = _unitOfWork.TaskComments.Query().FirstOrDefault(item => item.Id == id)
            ?? throw new NotFoundException("Không tìm thấy bình luận.");
        var task = await GetAccessibleTaskAsync(userId, comment.TaskId, cancellationToken);
        if (comment.AuthorId != userId && task.UserId != userId)
        {
            throw new AppException("Bạn không có quyền xóa bình luận này.", 403);
        }

        comment.IsDeleted = true;
        comment.UpdatedAt = DateTime.UtcNow;
        var activity = await _activityService.RecordAsync(
            comment.TaskId,
            userId,
            TaskActivityType.CommentDeleted,
            "đã xóa một bình luận",
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _notifier.CommentDeletedAsync(comment.TaskId, comment.Id, cancellationToken);
        await _notifier.ActivityAddedAsync(
            comment.TaskId,
            await _activityService.ToDtoAsync(activity, cancellationToken),
            cancellationToken);
    }

    private async Task<TodoTask> GetAccessibleTaskAsync(
        Guid userId,
        Guid taskId,
        CancellationToken cancellationToken) =>
        await _unitOfWork.Tasks.GetAccessibleForUserAsync(userId, taskId, includeDetails: true, cancellationToken)
        ?? throw new NotFoundException("Không tìm thấy công việc.");

    private void PopulateAuthors(IReadOnlyCollection<TaskComment> comments)
    {
        var authorIds = comments.Select(comment => comment.AuthorId).Distinct().ToArray();
        var authors = _unitOfWork.Users.Query()
            .Where(user => authorIds.Contains(user.Id))
            .ToDictionary(user => user.Id);
        foreach (var comment in comments)
        {
            comment.Author = authors[comment.AuthorId];
        }
    }

    private async Task NotifyCollaboratorsAsync(
        Guid actorId,
        TodoTask task,
        User actor,
        CancellationToken cancellationToken)
    {
        var recipientIds = task.Shares
            .Where(share => share.Status == ShareStatus.Accepted)
            .Select(share => share.SharedWithUserId)
            .Append(task.UserId)
            .Where(userId => userId != actorId)
            .Distinct();
        var actorName = string.IsNullOrWhiteSpace(actor.FullName) ? actor.Username : actor.FullName;
        foreach (var recipientId in recipientIds)
        {
            await _notificationService.CreateAsync(
                recipientId,
                task.Id,
                NotificationType.TaskCommented,
                $"{actorName} đã bình luận trong công việc: {task.Title}",
                cancellationToken);
        }
    }
}
