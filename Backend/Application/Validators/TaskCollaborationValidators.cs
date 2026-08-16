using FluentValidation;
using Application.DTOs;

namespace Application.Validators;

public class CreateTaskCommentRequestValidator : AbstractValidator<CreateTaskCommentRequest>
{
    public CreateTaskCommentRequestValidator()
    {
        RuleFor(request => request.Content)
            .NotEmpty().WithMessage("Nội dung bình luận là bắt buộc.")
            .MaximumLength(2000).WithMessage("Bình luận không được vượt quá 2000 ký tự.");
    }
}

public class UpdateTaskCommentRequestValidator : AbstractValidator<UpdateTaskCommentRequest>
{
    public UpdateTaskCommentRequestValidator()
    {
        RuleFor(request => request.Content)
            .NotEmpty().WithMessage("Nội dung bình luận là bắt buộc.")
            .MaximumLength(2000).WithMessage("Bình luận không được vượt quá 2000 ký tự.");
    }
}
