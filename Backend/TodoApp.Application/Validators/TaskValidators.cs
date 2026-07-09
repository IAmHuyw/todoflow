using FluentValidation;
using TodoApp.Application.DTOs;

namespace TodoApp.Application.Validators;

public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề công việc là bắt buộc.")
            .MaximumLength(200).WithMessage("Tiêu đề công việc không được vượt quá 200 ký tự.");
        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Mô tả công việc không được vượt quá 1000 ký tự.");
        RuleFor(x => x.TagIds).Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Danh sách nhãn không được trùng lặp.");
        RuleFor(x => x.RecurrenceType).IsInEnum().WithMessage("Kiểu lặp công việc không hợp lệ.");
        RuleFor(x => x.RecurrenceInterval)
            .InclusiveBetween(1, 365).WithMessage("Khoảng lặp phải nằm trong khoảng 1 đến 365.");
    }
}

public class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề công việc là bắt buộc.")
            .MaximumLength(200).WithMessage("Tiêu đề công việc không được vượt quá 200 ký tự.");
        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Mô tả công việc không được vượt quá 1000 ký tự.");
        RuleFor(x => x.TagIds).Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Danh sách nhãn không được trùng lặp.");
        RuleFor(x => x.RecurrenceType).IsInEnum().WithMessage("Kiểu lặp công việc không hợp lệ.");
        RuleFor(x => x.RecurrenceInterval)
            .InclusiveBetween(1, 365).WithMessage("Khoảng lặp phải nằm trong khoảng 1 đến 365.");
    }
}

public class UpdateTaskStatusRequestValidator : AbstractValidator<UpdateTaskStatusRequest>
{
    public UpdateTaskStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum().WithMessage("Trạng thái công việc không hợp lệ.");
    }
}

public class ReorderTasksRequestValidator : AbstractValidator<ReorderTasksRequest>
{
    public ReorderTasksRequestValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Danh sách sắp xếp công việc là bắt buộc.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.Id).NotEmpty().WithMessage("Mã công việc là bắt buộc.");
            item.RuleFor(x => x.Status).IsInEnum().WithMessage("Trạng thái công việc không hợp lệ.");
            item.RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0)
                .WithMessage("Thứ tự công việc không hợp lệ.");
        });
        RuleFor(x => x.Items)
            .Must(items => items.Select(item => item.Id).Distinct().Count() == items.Count)
            .WithMessage("Danh sách công việc không được trùng lặp.");
    }
}
