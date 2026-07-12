using FluentValidation;
using Application.DTOs;
using Domain.Enums;

namespace Application.Validators;

public class ShareTaskRequestValidator : AbstractValidator<ShareTaskRequest>
{
    public ShareTaskRequestValidator()
    {
        RuleFor(x => x.EmailOrUsername)
            .NotEmpty().WithMessage("Email hoặc tên đăng nhập là bắt buộc.")
            .MaximumLength(100).WithMessage("Email hoặc tên đăng nhập không được vượt quá 100 ký tự.");
        RuleFor(x => x.Permission).IsInEnum().WithMessage("Quyền chia sẻ không hợp lệ.");
    }
}

public class RespondShareRequestValidator : AbstractValidator<RespondShareRequest>
{
    public RespondShareRequestValidator()
    {
        RuleFor(x => x.Status)
            .Must(status => status is ShareStatus.Accepted or ShareStatus.Rejected)
            .WithMessage("Phản hồi chia sẻ phải là chấp nhận hoặc từ chối.");
    }
}

public class ChangeSharePermissionRequestValidator : AbstractValidator<ChangeSharePermissionRequest>
{
    public ChangeSharePermissionRequestValidator()
    {
        RuleFor(x => x.Permission).IsInEnum().WithMessage("Quyền chia sẻ không hợp lệ.");
    }
}
