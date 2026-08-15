using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Enums;

namespace Api.Controllers;

[Authorize(Roles = nameof(UserRole.Admin))]
[Route("api/admin/users")]
public class AdminUsersController : ApiControllerBase
{
    private readonly IAdminUserService _adminUserService;

    public AdminUsersController(IAdminUserService adminUserService)
    {
        _adminUserService = adminUserService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AdminUserDto>>>> GetAll(
        [FromQuery] AdminUserQueryParameters query,
        CancellationToken cancellationToken)
    {
        var users = await _adminUserService.GetAllAsync(query, cancellationToken);
        return OkResponse(users);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<AdminUserDto>>> UpdateStatus(
        Guid id,
        UpdateUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _adminUserService.UpdateStatusAsync(
            CurrentUserId,
            id,
            request,
            cancellationToken);
        return OkResponse(user, request.IsActive ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản.");
    }
}
