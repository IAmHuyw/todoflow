using FluentValidation;
using TodoApp.Application.DTOs;

namespace TodoApp.Application.Validators;

public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên danh mục là bắt buộc.")
            .MaximumLength(50).WithMessage("Tên danh mục không được vượt quá 50 ký tự.");
        RuleFor(x => x.Color)
            .NotEmpty().WithMessage("Màu danh mục là bắt buộc.")
            .MaximumLength(20).WithMessage("Màu danh mục không được vượt quá 20 ký tự.")
            .Matches("^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$")
            .WithMessage("Màu danh mục phải là mã hex hợp lệ.");
    }
}

public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên danh mục là bắt buộc.")
            .MaximumLength(50).WithMessage("Tên danh mục không được vượt quá 50 ký tự.");
        RuleFor(x => x.Color)
            .NotEmpty().WithMessage("Màu danh mục là bắt buộc.")
            .MaximumLength(20).WithMessage("Màu danh mục không được vượt quá 20 ký tự.")
            .Matches("^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$")
            .WithMessage("Màu danh mục phải là mã hex hợp lệ.");
    }
}
