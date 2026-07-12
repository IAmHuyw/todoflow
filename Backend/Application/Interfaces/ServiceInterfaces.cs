using Application.Common;
using Application.DTOs;

namespace Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default);
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task<UserDto> GetMeAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
}

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<CategoryDto> CreateAsync(Guid userId, CreateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<CategoryDto> UpdateAsync(Guid userId, Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
}

public interface ITagService
{
    Task<IReadOnlyList<TagDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<TagDto> CreateAsync(Guid userId, CreateTagRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
}

public interface ITaskService
{
    Task<PagedResult<TaskDto>> GetAllAsync(Guid userId, TaskQueryParameters query, CancellationToken cancellationToken = default);
    Task<TaskDto> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
    Task<TaskDto> CreateAsync(Guid userId, CreateTaskRequest request, CancellationToken cancellationToken = default);
    Task<TaskDto> UpdateAsync(Guid userId, Guid id, UpdateTaskRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
    Task<TaskDto> UpdateStatusAsync(Guid userId, Guid id, UpdateTaskStatusRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskDto>> ReorderAsync(Guid userId, ReorderTasksRequest request, CancellationToken cancellationToken = default);
}

public interface ITaskShareService
{
    Task<TaskShareDto> ShareAsync(Guid ownerId, Guid taskId, ShareTaskRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskShareDto>> GetSharesForTaskAsync(Guid ownerId, Guid taskId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskShareDto>> GetSharedWithMeAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<TaskShareDto> RespondAsync(Guid userId, Guid shareId, RespondShareRequest request, CancellationToken cancellationToken = default);
    Task<TaskShareDto> ChangePermissionAsync(Guid ownerId, Guid shareId, ChangeSharePermissionRequest request, CancellationToken cancellationToken = default);
    Task RevokeAsync(Guid ownerId, Guid shareId, CancellationToken cancellationToken = default);
}

public interface ISubTaskService
{
    Task<SubTaskDto> CreateAsync(Guid userId, Guid taskId, CreateSubTaskRequest request, CancellationToken cancellationToken = default);
    Task<SubTaskDto> UpdateAsync(Guid userId, Guid id, UpdateSubTaskRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
}

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetAllAsync(Guid userId, NotificationQueryParameters query, CancellationToken cancellationToken = default);
    Task<NotificationDto> CreateAsync(Guid userId, Guid? taskId, Domain.Enums.NotificationType type, string message, CancellationToken cancellationToken = default);
    Task MarkReadAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
    Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
    Task DeleteAllAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IReminderService
{
    Task<IReadOnlyList<TaskReminderDto>> GetForTaskAsync(Guid userId, Guid taskId, CancellationToken cancellationToken = default);
    Task<TaskReminderDto> CreateAsync(Guid userId, Guid taskId, CreateReminderRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
    Task<int> ProcessDueAsync(CancellationToken cancellationToken = default);
}
