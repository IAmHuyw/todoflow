using FluentValidation;
using TodoApp.Application.DTOs;

namespace TodoApp.Application.Validators;

public class CreateTagRequestValidator : AbstractValidator<CreateTagRequest>
{
    public CreateTagRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên tag là bắt buộc.")
            .MaximumLength(50).WithMessage("Tên tag không được vượt quá 50 ký tự.");
    }
}
