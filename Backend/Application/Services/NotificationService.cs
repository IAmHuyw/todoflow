using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRealtimeNotifier _notifier;

    public NotificationService(IUnitOfWork unitOfWork, IRealtimeNotifier notifier)
    {
        _unitOfWork = unitOfWork;
        _notifier = notifier;
    }

    public Task<PagedResult<NotificationDto>> GetAllAsync(
        Guid userId,
        NotificationQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var notifications = _unitOfWork.Notifications.Query()
            .Where(notification => notification.UserId == userId)
            .OrderByDescending(notification => notification.CreatedAt);

        var totalCount = notifications.Count();
        var items = notifications
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArray()
            .Select(DtoMapper.ToDto)
            .ToArray();

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        return Task.FromResult(new PagedResult<NotificationDto>(items, page, pageSize, totalCount, totalPages));
    }

    public async Task<NotificationDto> CreateAsync(
        Guid userId,
        Guid? taskId,
        NotificationType type,
        string message,
        CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            TaskId = taskId,
            Type = type,
            Message = message.Trim(),
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Notifications.AddAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = DtoMapper.ToDto(notification);
        await _notifier.NotificationReceivedAsync(dto, cancellationToken);
        return dto;
    }

    public async Task MarkReadAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        var notification = _unitOfWork.Notifications.Query()
            .FirstOrDefault(notification => notification.Id == id && notification.UserId == userId)
            ?? throw new NotFoundException("Không tìm thấy thông báo.");

        notification.IsRead = true;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var notifications = _unitOfWork.Notifications.Query()
            .Where(notification => notification.UserId == userId && !notification.IsRead)
            .ToArray();

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        var notification = _unitOfWork.Notifications.Query()
            .FirstOrDefault(item => item.Id == id && item.UserId == userId)
            ?? throw new NotFoundException("Không tìm thấy thông báo.");

        _unitOfWork.Notifications.Remove(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var notifications = _unitOfWork.Notifications.Query()
            .Where(notification => notification.UserId == userId)
            .ToArray();

        foreach (var notification in notifications)
        {
            _unitOfWork.Notifications.Remove(notification);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
