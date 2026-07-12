using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Common;
using Application.DTOs;
using Application.Interfaces;

namespace Api.Controllers;

[Authorize]
[Route("api/[controller]")]
public class TagsController : ApiControllerBase
{
    private readonly ITagService _tagService;

    public TagsController(ITagService tagService)
    {
        _tagService = tagService;
    }

    // Lists tags owned by the authenticated user.
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TagDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var tags = await _tagService.GetAllAsync(CurrentUserId, cancellationToken);
        return OkResponse(tags);
    }

    // Creates a tag owned by the authenticated user.
    [HttpPost]
    public async Task<ActionResult<ApiResponse<TagDto>>> Create(
        CreateTagRequest request,
        CancellationToken cancellationToken)
    {
        var tag = await _tagService.CreateAsync(CurrentUserId, request, cancellationToken);
        return OkResponse(tag, "Đã tạo nhãn.");
    }

    // Deletes a tag and its task associations.
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _tagService.DeleteAsync(CurrentUserId, id, cancellationToken);
        return OkMessage("Đã xoá nhãn.");
    }
}
