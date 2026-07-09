using FluentValidation;
using TodoApp.Application.Common;
using TodoApp.Application.DTOs;
using TodoApp.Application.Interfaces;
using TodoApp.Domain.Entities;

namespace TodoApp.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateCategoryRequest> _createValidator;
    private readonly IValidator<UpdateCategoryRequest> _updateValidator;

    public CategoryService(
        IUnitOfWork unitOfWork,
        IValidator<CreateCategoryRequest> createValidator,
        IValidator<UpdateCategoryRequest> updateValidator)
    {
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public Task<IReadOnlyList<CategoryDto>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var categories = _unitOfWork.Categories.Query()
            .Where(category => category.UserId == userId)
            .OrderBy(category => category.Name)
            .Select(DtoMapper.ToDto)
            .ToArray();

        return Task.FromResult<IReadOnlyList<CategoryDto>>(categories);
    }

    public async Task<CategoryDto> CreateAsync(
        Guid userId,
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        await _createValidator.EnsureValidAsync(request, cancellationToken);

        var name = request.Name.Trim();
        EnsureNameIsUnique(userId, name);

        var category = new Category
        {
            UserId = userId,
            Name = name,
            Color = request.Color.Trim()
        };

        await _unitOfWork.Categories.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DtoMapper.ToDto(category);
    }

    public async Task<CategoryDto> UpdateAsync(
        Guid userId,
        Guid id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        await _updateValidator.EnsureValidAsync(request, cancellationToken);

        var category = _unitOfWork.Categories.Query()
            .FirstOrDefault(category => category.Id == id && category.UserId == userId)
            ?? throw new NotFoundException("Không tìm thấy category.");

        var name = request.Name.Trim();
        EnsureNameIsUnique(userId, name, id);

        category.Name = name;
        category.Color = request.Color.Trim();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return DtoMapper.ToDto(category);
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        var category = _unitOfWork.Categories.Query()
            .FirstOrDefault(category => category.Id == id && category.UserId == userId)
            ?? throw new NotFoundException("Không tìm thấy category.");

        var tasks = _unitOfWork.Tasks.QueryForUser(userId)
            .Where(task => task.CategoryId == id)
            .ToArray();

        foreach (var task in tasks)
        {
            task.CategoryId = null;
            task.UpdatedAt = DateTime.UtcNow;
        }

        _unitOfWork.Categories.Remove(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private void EnsureNameIsUnique(Guid userId, string name, Guid? ignoredCategoryId = null)
    {
        var exists = _unitOfWork.Categories.Query().Any(category =>
            category.UserId == userId &&
            category.Id != ignoredCategoryId &&
            category.Name.ToLower() == name.ToLower());

        if (exists)
        {
            throw new AppException("Tên category đã tồn tại.", 409);
        }
    }
}
