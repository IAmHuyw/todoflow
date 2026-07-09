using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Application.Common;
using TodoApp.Application.DTOs;
using TodoApp.Application.Interfaces;

namespace TodoApp.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
public class CategoriesController : ApiControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    // Lists categories owned by the authenticated user.
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CategoryDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var categories = await _categoryService.GetAllAsync(CurrentUserId, cancellationToken);
        return OkResponse(categories);
    }

    // Creates a category for the authenticated user.
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> Create(
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await _categoryService.CreateAsync(CurrentUserId, request, cancellationToken);
        return OkResponse(category, "Đã tạo danh mục.");
    }

    // Updates a category owned by the authenticated user.
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> Update(
        Guid id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await _categoryService.UpdateAsync(CurrentUserId, id, request, cancellationToken);
        return OkResponse(category, "Đã cập nhật danh mục.");
    }

    // Deletes a category and clears it from existing tasks.
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _categoryService.DeleteAsync(CurrentUserId, id, cancellationToken);
        return OkMessage("Đã xoá danh mục.");
    }
}
