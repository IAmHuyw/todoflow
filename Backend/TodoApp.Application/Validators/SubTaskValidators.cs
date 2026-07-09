using FluentValidation;
using TodoApp.Application.DTOs;

namespace TodoApp.Application.Validators;

public class CreateSubTaskRequestValidator : AbstractValidator<CreateSubTaskRequest>
{
    public CreateSubTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề subtask là bắt buộc.")
            .MaximumLength(200).WithMessage("Tiêu đề subtask không được vượt quá 200 ký tự.");
    }
}

public class UpdateSubTaskRequestValidator : AbstractValidator<UpdateSubTaskRequest>
{
    public UpdateSubTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề subtask là bắt buộc.")
            .MaximumLength(200).WithMessage("Tiêu đề subtask không được vượt quá 200 ký tự.");
    }
}
