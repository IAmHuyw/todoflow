using FluentValidation;
using TodoApp.Application.DTOs;

namespace TodoApp.Application.Validators;

public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề task là bắt buộc.")
            .MaximumLength(200).WithMessage("Tiêu đề task không được vượt quá 200 ký tự.");
        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Mô tả task không được vượt quá 1000 ký tự.");
        RuleFor(x => x.TagIds).Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Danh sách tag không được trùng lặp.");
    }
}

public class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề task là bắt buộc.")
            .MaximumLength(200).WithMessage("Tiêu đề task không được vượt quá 200 ký tự.");
        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Mô tả task không được vượt quá 1000 ký tự.");
        RuleFor(x => x.TagIds).Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Danh sách tag không được trùng lặp.");
    }
}

public class UpdateTaskStatusRequestValidator : AbstractValidator<UpdateTaskStatusRequest>
{
    public UpdateTaskStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum().WithMessage("Trạng thái task không hợp lệ.");
    }
}
