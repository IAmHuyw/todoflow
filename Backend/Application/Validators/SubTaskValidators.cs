using FluentValidation;
using Application.DTOs;

namespace Application.Validators;

public class CreateSubTaskRequestValidator : AbstractValidator<CreateSubTaskRequest>
{
    public CreateSubTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề việc con là bắt buộc.")
            .MaximumLength(200).WithMessage("Tiêu đề việc con không được vượt quá 200 ký tự.");

        RuleFor(x => x.Note)
            .MaximumLength(1000).WithMessage("Ghi chú việc con không được vượt quá 1000 ký tự.");
    }
}

public class UpdateSubTaskRequestValidator : AbstractValidator<UpdateSubTaskRequest>
{
    public UpdateSubTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề việc con là bắt buộc.")
            .MaximumLength(200).WithMessage("Tiêu đề việc con không được vượt quá 200 ký tự.");

        RuleFor(x => x.Note)
            .MaximumLength(1000).WithMessage("Ghi chú việc con không được vượt quá 1000 ký tự.");
    }
}
