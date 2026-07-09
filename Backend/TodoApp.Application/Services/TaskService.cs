using FluentValidation;
using TodoApp.Application.Common;
using TodoApp.Application.DTOs;
using TodoApp.Application.Interfaces;
using TodoApp.Domain.Entities;
using TodoApp.Domain.Enums;

namespace TodoApp.Application.Services;

public class TaskService : ITaskService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateTaskRequest> _createValidator;
    private readonly IValidator<UpdateTaskRequest> _updateValidator;
    private readonly IValidator<UpdateTaskStatusRequest> _statusValidator;
    private readonly IRealtimeNotifier _notifier;
    private readonly INotificationService? _notificationService;

    public TaskService(
        IUnitOfWork unitOfWork,
        IValidator<CreateTaskRequest> createValidator,
        IValidator<UpdateTaskRequest> updateValidator,
        IValidator<UpdateTaskStatusRequest> statusValidator,
        IRealtimeNotifier? notifier = null,
        INotificationService? notificationService = null)
    {
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _statusValidator = statusValidator;
        _notifier = notifier ?? new NoopRealtimeNotifier();
        _notificationService = notificationService;
    }

    public Task<PagedResult<TaskDto>> GetAllAsync(
        Guid userId,
        TaskQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var priority = EnumParser.ParsePriority(query.Priority);
        var status = EnumParser.ParseStatus(query.Status);

        var tasks = _unitOfWork.Tasks.QueryAccessibleForUser(userId, includeDetails: true);

        if (query.CategoryId.HasValue)
        {
            tasks = tasks.Where(task => task.CategoryId == query.CategoryId.Value);
        }

        if (priority.HasValue)
        {
            tasks = tasks.Where(task => task.Priority == priority.Value);
        }

        if (status.HasValue)
        {
            tasks = tasks.Where(task => task.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            tasks = tasks.Where(task =>
                task.Title.ToLower().Contains(search) ||
                (task.Description != null && task.Description.ToLower().Contains(search)));
        }

        tasks = ApplySort(tasks, query.SortBy, query.SortDir);

        var totalCount = tasks.Count();
        var items = tasks
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArray()
            .Select(DtoMapper.ToDto)
            .ToArray();

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        return Task.FromResult(new PagedResult<TaskDto>(items, page, pageSize, totalCount, totalPages));
    }

    public async Task<TaskDto> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _unitOfWork.Tasks.GetAccessibleForUserAsync(userId, id, includeDetails: true, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy task.");

        return DtoMapper.ToDto(task);
    }

    public async Task<TaskDto> CreateAsync(
        Guid userId,
        CreateTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        await _createValidator.EnsureValidAsync(request, cancellationToken);
        EnsureCategoryBelongsToUser(userId, request.CategoryId);
        EnsureTagsBelongToUser(userId, request.TagIds);

        var now = DateTime.UtcNow;
        var task = new TodoTask
        {
            UserId = userId,
            CategoryId = request.CategoryId,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim(),
            Priority = request.Priority,
            Status = request.Status,
            DueDate = request.DueDate,
            CreatedAt = now,
            UpdatedAt = now,
            TaskTags = request.TagIds.Select(tagId => new TaskTag { TagId = tagId }).ToList()
        };

        await _unitOfWork.Tasks.AddAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DtoMapper.ToDto(task);
    }

    public async Task<TaskDto> UpdateAsync(
        Guid userId,
        Guid id,
        UpdateTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        await _updateValidator.EnsureValidAsync(request, cancellationToken);

        var task = await _unitOfWork.Tasks.GetAccessibleForUserAsync(userId, id, includeDetails: true, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy task.");

        EnsureCanEdit(userId, task);

        EnsureCategoryBelongsToUser(task.UserId, request.CategoryId);
        EnsureTagsBelongToUser(task.UserId, request.TagIds);

        task.CategoryId = request.CategoryId;
        task.Title = request.Title.Trim();
        task.Description = string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim();
        task.Priority = request.Priority;
        task.Status = request.Status;
        task.DueDate = request.DueDate;
        task.UpdatedAt = DateTime.UtcNow;

        task.TaskTags.Clear();
        foreach (var tagId in request.TagIds)
        {
            task.TaskTags.Add(new TaskTag { TaskId = task.Id, TagId = tagId });
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var dto = DtoMapper.ToDto(task);
        await _notifier.TaskUpdatedAsync(task.Id, dto, cancellationToken);
        await NotifyTaskWatchersAsync(userId, task, NotificationType.TaskUpdated, $"Task đã được cập nhật: {task.Title}", cancellationToken);
        return dto;
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _unitOfWork.Tasks.GetForUserAsync(userId, id, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy task.");

        task.IsDeleted = true;
        task.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _notifier.TaskDeletedAsync(task.Id, cancellationToken);
    }

    public async Task<TaskDto> UpdateStatusAsync(
        Guid userId,
        Guid id,
        UpdateTaskStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        await _statusValidator.EnsureValidAsync(request, cancellationToken);

        var task = await _unitOfWork.Tasks.GetAccessibleForUserAsync(userId, id, includeDetails: true, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy task.");

        EnsureCanEdit(userId, task);

        task.Status = request.Status;
        task.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var dto = DtoMapper.ToDto(task);
        await _notifier.TaskStatusChangedAsync(task.Id, task.Status, cancellationToken);
        await _notifier.TaskUpdatedAsync(task.Id, dto, cancellationToken);
        if (task.Status == TodoStatus.Done)
        {
            await NotifyTaskWatchersAsync(userId, task, NotificationType.TaskCompleted, $"Task đã hoàn thành: {task.Title}", cancellationToken);
        }
        else
        {
            await NotifyTaskWatchersAsync(userId, task, NotificationType.TaskUpdated, $"Trạng thái task đã thay đổi: {task.Title}", cancellationToken);
        }
        return dto;
    }

    private static IQueryable<TodoTask> ApplySort(
        IQueryable<TodoTask> query,
        string? sortBy,
        string? sortDir)
    {
        var ascending = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);

        return (sortBy ?? "createdAt").Trim().ToLowerInvariant() switch
        {
            "duedate" => ascending
                ? query.OrderBy(task => task.DueDate ?? DateTime.MaxValue).ThenBy(task => task.CreatedAt)
                : query.OrderByDescending(task => task.DueDate ?? DateTime.MinValue).ThenByDescending(task => task.CreatedAt),
            "priority" => ascending
                ? query.OrderBy(task => task.Priority == Priority.High ? 0 : task.Priority == Priority.Medium ? 1 : 2)
                    .ThenByDescending(task => task.CreatedAt)
                : query.OrderByDescending(task => task.Priority == Priority.High ? 0 : task.Priority == Priority.Medium ? 1 : 2)
                    .ThenByDescending(task => task.CreatedAt),
            "title" => ascending
                ? query.OrderBy(task => task.Title)
                : query.OrderByDescending(task => task.Title),
            "createdat" => ascending
                ? query.OrderBy(task => task.CreatedAt)
                : query.OrderByDescending(task => task.CreatedAt),
            _ => throw new AppException("Giá trị sortBy không hợp lệ.", 400)
        };
    }

    private void EnsureCategoryBelongsToUser(Guid userId, Guid? categoryId)
    {
        if (!categoryId.HasValue)
        {
            return;
        }

        var exists = _unitOfWork.Categories.Query()
            .Any(category => category.Id == categoryId.Value && category.UserId == userId);

        if (!exists)
        {
            throw new AppException("Category không thuộc về người dùng hiện tại.", 400);
        }
    }

    private void EnsureTagsBelongToUser(Guid userId, IReadOnlyCollection<Guid> tagIds)
    {
        if (tagIds.Count == 0)
        {
            return;
        }

        var foundCount = _unitOfWork.Tags.Query()
            .Count(tag => tag.UserId == userId && tagIds.Contains(tag.Id));

        if (foundCount != tagIds.Count)
        {
            throw new AppException("Một hoặc nhiều tag không thuộc về người dùng hiện tại.", 400);
        }
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

    private async Task NotifyTaskWatchersAsync(
        Guid actorId,
        TodoTask task,
        NotificationType type,
        string message,
        CancellationToken cancellationToken)
    {
        if (_notificationService is null)
        {
            return;
        }

        var userIds = task.Shares
            .Where(share => share.Status == ShareStatus.Accepted)
            .Select(share => share.SharedWithUserId)
            .Append(task.UserId)
            .Where(userId => userId != actorId)
            .Distinct()
            .ToArray();

        foreach (var userId in userIds)
        {
            await _notificationService.CreateAsync(userId, task.Id, type, message, cancellationToken);
        }
    }
}
