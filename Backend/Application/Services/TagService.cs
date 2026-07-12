using System.Text.RegularExpressions;
using FluentValidation;
using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Services;

public class TagService : ITagService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateTagRequest> _createValidator;

    public TagService(IUnitOfWork unitOfWork, IValidator<CreateTagRequest> createValidator)
    {
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
    }

    public Task<IReadOnlyList<TagDto>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var tags = _unitOfWork.Tags.Query()
            .Where(tag => tag.UserId == userId)
            .OrderBy(tag => tag.Name)
            .Select(DtoMapper.ToDto)
            .ToArray();

        return Task.FromResult<IReadOnlyList<TagDto>>(tags);
    }

    public async Task<TagDto> CreateAsync(
        Guid userId,
        CreateTagRequest request,
        CancellationToken cancellationToken = default)
    {
        await _createValidator.EnsureValidAsync(request, cancellationToken);

        var name = NormalizeTagName(request.Name);
        if (_unitOfWork.Tags.Query().Any(tag => tag.UserId == userId && tag.Name.ToLower() == name))
        {
            throw new AppException("Tên nhãn đã tồn tại.", 409);
        }

        var tag = new Tag
        {
            UserId = userId,
            Name = name
        };

        await _unitOfWork.Tags.AddAsync(tag, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DtoMapper.ToDto(tag);
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        var tag = _unitOfWork.Tags.Query()
            .FirstOrDefault(tag => tag.Id == id && tag.UserId == userId)
            ?? throw new NotFoundException("Không tìm thấy nhãn.");

        _unitOfWork.Tags.Remove(tag);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeTagName(string name) =>
        Regex.Replace(name.Trim().ToLowerInvariant(), "\\s+", "-");
}
