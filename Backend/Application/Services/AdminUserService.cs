using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public class AdminUserService : IAdminUserService
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminUserService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<PagedResult<AdminUserDto>> GetAllAsync(
        AdminUserQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var users = _unitOfWork.Users.Query();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            users = users.Where(user =>
                user.Username.ToLower().Contains(search) ||
                user.Email.ToLower().Contains(search) ||
                (user.FullName != null && user.FullName.ToLower().Contains(search)));
        }

        if (query.IsActive.HasValue)
        {
            users = users.Where(user => user.IsActive == query.IsActive.Value);
        }

        users = users
            .OrderByDescending(user => user.CreatedAt)
            .ThenBy(user => user.Username);

        var totalCount = users.Count();
        var items = users
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArray()
            .Select(ToDto)
            .ToArray();
        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        return Task.FromResult(new PagedResult<AdminUserDto>(items, page, pageSize, totalCount, totalPages));
    }

    public async Task<AdminUserDto> UpdateStatusAsync(
        Guid adminId,
        Guid userId,
        UpdateUserStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (adminId == userId)
        {
            throw new AppException("Bạn không thể thay đổi trạng thái tài khoản của chính mình.", 400);
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy người dùng.");

        if (user.Role == UserRole.Admin)
        {
            throw new AppException("Không thể thay đổi trạng thái tài khoản quản trị.", 400);
        }

        if (user.IsActive == request.IsActive)
        {
            return ToDto(user);
        }

        user.IsActive = request.IsActive;
        if (!user.IsActive)
        {
            var now = DateTime.UtcNow;
            var activeTokens = _unitOfWork.RefreshTokens.Query()
                .Where(token => token.UserId == user.Id && token.RevokedAt == null)
                .ToArray();

            foreach (var token in activeTokens)
            {
                token.RevokedAt = now;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(user);
    }

    private static AdminUserDto ToDto(User user) =>
        new(
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            user.Role,
            user.IsActive,
            user.CreatedAt);
}
