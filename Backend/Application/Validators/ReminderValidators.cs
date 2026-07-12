using FluentValidation;
using Application.DTOs;

namespace Application.Validators;

public class CreateReminderRequestValidator : AbstractValidator<CreateReminderRequest>
{
    public CreateReminderRequestValidator()
    {
        RuleFor(x => x.RemindAt)
            .Must(value => value >= DateTime.UtcNow.AddMinutes(-1))
            .WithMessage("Thời điểm nhắc nhở phải ở hiện tại hoặc tương lai.");
        RuleFor(x => x.Channel)
            .IsInEnum()
            .WithMessage("Kênh nhắc nhở không hợp lệ.");
    }
}
