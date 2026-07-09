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
    private readonly IValidator<ReorderTasksRequest> _reorderValidator;
    private readonly IRealtimeNotifier _notifier;
    private readonly INotificationService? _notificationService;

    public TaskService(
        IUnitOfWork unitOfWork,
        IValidator<CreateTaskRequest> createValidator,
        IValidator<UpdateTaskRequest> updateValidator,
        IValidator<UpdateTaskStatusRequest> statusValidator,
        IValidator<ReorderTasksRequest> reorderValidator,
        IRealtimeNotifier? notifier = null,
        INotificationService? notificationService = null)
    {
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _statusValidator = statusValidator;
        _reorderValidator = reorderValidator;
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
            ?? throw new NotFoundException("Không tìm thấy công việc.");

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
            RecurrenceType = request.RecurrenceType,
            RecurrenceInterval = request.RecurrenceType == RecurrenceType.None ? 1 : request.RecurrenceInterval,
            RecurrenceEndDate = request.RecurrenceType == RecurrenceType.None ? null : request.RecurrenceEndDate,
            SortOrder = GetNextSortOrder(userId, request.Status),
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
            ?? throw new NotFoundException("Không tìm thấy công việc.");

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
        task.RecurrenceType = request.RecurrenceType;
        task.RecurrenceInterval = request.RecurrenceType == RecurrenceType.None ? 1 : request.RecurrenceInterval;
        task.RecurrenceEndDate = request.RecurrenceType == RecurrenceType.None ? null : request.RecurrenceEndDate;
        task.UpdatedAt = DateTime.UtcNow;

        task.TaskTags.Clear();
        foreach (var tagId in request.TagIds)
        {
            task.TaskTags.Add(new TaskTag { TaskId = task.Id, TagId = tagId });
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var dto = DtoMapper.ToDto(task);
        await _notifier.TaskUpdatedAsync(task.Id, dto, cancellationToken);
        await NotifyTaskWatchersAsync(userId, task, NotificationType.TaskUpdated, $"Công việc đã được cập nhật: {task.Title}", cancellationToken);
        return dto;
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _unitOfWork.Tasks.GetForUserAsync(userId, id, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy công việc.");

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
            ?? throw new NotFoundException("Không tìm thấy công việc.");

        EnsureCanEdit(userId, task);

        var previousStatus = task.Status;
        task.Status = request.Status;
        task.UpdatedAt = DateTime.UtcNow;

        if (previousStatus != TodoStatus.Done && request.Status == TodoStatus.Done)
        {
            var nextRecurringTask = CreateNextRecurringTaskIfNeeded(task);
            if (nextRecurringTask is not null)
            {
                await _unitOfWork.Tasks.AddAsync(nextRecurringTask, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var dto = DtoMapper.ToDto(task);
        await _notifier.TaskStatusChangedAsync(task.Id, task.Status, cancellationToken);
        await _notifier.TaskUpdatedAsync(task.Id, dto, cancellationToken);
        if (task.Status == TodoStatus.Done)
        {
            await NotifyTaskWatchersAsync(userId, task, NotificationType.TaskCompleted, $"Công việc đã hoàn thành: {task.Title}", cancellationToken);
        }
        else
        {
            await NotifyTaskWatchersAsync(userId, task, NotificationType.TaskUpdated, $"Trạng thái công việc đã thay đổi: {task.Title}", cancellationToken);
        }
        return dto;
    }

    public async Task<IReadOnlyList<TaskDto>> ReorderAsync(
        Guid userId,
        ReorderTasksRequest request,
        CancellationToken cancellationToken = default)
    {
        await _reorderValidator.EnsureValidAsync(request, cancellationToken);

        var ids = request.Items.Select(item => item.Id).ToArray();
        var tasks = _unitOfWork.Tasks.QueryAccessibleForUser(userId, includeDetails: true)
            .Where(task => ids.Contains(task.Id))
            .ToArray();

        if (tasks.Length != ids.Length)
        {
            throw new AppException("Một hoặc nhiều công việc không tồn tại hoặc bạn không có quyền truy cập.", 404);
        }

        var itemById = request.Items.ToDictionary(item => item.Id);
        var now = DateTime.UtcNow;
        foreach (var task in tasks)
        {
            EnsureCanEdit(userId, task);

            var item = itemById[task.Id];
            task.Status = item.Status;
            task.SortOrder = item.SortOrder;
            task.UpdatedAt = now;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dtos = tasks.Select(DtoMapper.ToDto).ToArray();
        foreach (var dto in dtos)
        {
            await _notifier.TaskUpdatedAsync(dto.Id, dto, cancellationToken);
        }

        return dtos;
    }

    private static IQueryable<TodoTask> ApplySort(
        IQueryable<TodoTask> query,
        string? sortBy,
        string? sortDir)
    {
        var ascending = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);

        return NormalizeSortBy(sortBy) switch
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
            "sortorder" => ascending
                ? query.OrderBy(task => task.Status).ThenBy(task => task.SortOrder).ThenByDescending(task => task.CreatedAt)
                : query.OrderByDescending(task => task.Status).ThenByDescending(task => task.SortOrder).ThenByDescending(task => task.CreatedAt),
            "createdat" => ascending
                ? query.OrderBy(task => task.CreatedAt)
                : query.OrderByDescending(task => task.CreatedAt),
            _ => throw new AppException("Giá trị sortBy không hợp lệ.", 400)
        };
    }

    private static string NormalizeSortBy(string? sortBy) =>
        (sortBy ?? "sortOrder")
            .Trim()
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();

    private int GetNextSortOrder(Guid userId, TodoStatus status)
    {
        var orders = _unitOfWork.Tasks.Query()
            .Where(task => task.UserId == userId && task.Status == status)
            .Select(task => task.SortOrder)
            .ToArray();

        return orders.Length == 0 ? 0 : orders.Max() + 1;
    }

    private TodoTask? CreateNextRecurringTaskIfNeeded(TodoTask completedTask)
    {
        if (completedTask.RecurrenceType == RecurrenceType.None || !completedTask.DueDate.HasValue)
        {
            return null;
        }

        var nextDueDate = completedTask.RecurrenceType switch
        {
            RecurrenceType.Daily => completedTask.DueDate.Value.AddDays(completedTask.RecurrenceInterval),
            RecurrenceType.Weekly => completedTask.DueDate.Value.AddDays(7 * completedTask.RecurrenceInterval),
            RecurrenceType.Monthly => completedTask.DueDate.Value.AddMonths(completedTask.RecurrenceInterval),
            _ => (DateTime?)null
        };

        if (!nextDueDate.HasValue ||
            (completedTask.RecurrenceEndDate.HasValue && nextDueDate.Value > completedTask.RecurrenceEndDate.Value))
        {
            return null;
        }

        var now = DateTime.UtcNow;
        return new TodoTask
        {
            UserId = completedTask.UserId,
            CategoryId = completedTask.CategoryId,
            Title = completedTask.Title,
            Description = completedTask.Description,
            Priority = completedTask.Priority,
            Status = TodoStatus.Todo,
            DueDate = nextDueDate.Value,
            RecurrenceType = completedTask.RecurrenceType,
            RecurrenceInterval = completedTask.RecurrenceInterval,
            RecurrenceEndDate = completedTask.RecurrenceEndDate,
            RecurrenceParentId = completedTask.RecurrenceParentId ?? completedTask.Id,
            SortOrder = GetNextSortOrder(completedTask.UserId, TodoStatus.Todo),
            CreatedAt = now,
            UpdatedAt = now,
            TaskTags = completedTask.TaskTags
                .Select(taskTag => new TaskTag { TagId = taskTag.TagId })
                .ToList()
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
            throw new AppException("Danh mục không thuộc về người dùng hiện tại.", 400);
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
            throw new AppException("Một hoặc nhiều nhãn không thuộc về người dùng hiện tại.", 400);
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
            throw new AppException("Bạn không có quyền chỉnh sửa công việc này.", 403);
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
