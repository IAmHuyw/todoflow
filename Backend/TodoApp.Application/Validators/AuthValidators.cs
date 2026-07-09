using FluentValidation;
using TodoApp.Application.DTOs;

namespace TodoApp.Application.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Tên đăng nhập là bắt buộc.")
            .MinimumLength(3).WithMessage("Tên đăng nhập phải có ít nhất 3 ký tự.")
            .MaximumLength(50).WithMessage("Tên đăng nhập không được vượt quá 50 ký tự.");
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email là bắt buộc.")
            .EmailAddress().WithMessage("Email không hợp lệ.")
            .MaximumLength(100).WithMessage("Email không được vượt quá 100 ký tự.");
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu là bắt buộc.")
            .MinimumLength(6).WithMessage("Mật khẩu phải có ít nhất 6 ký tự.")
            .MaximumLength(100).WithMessage("Mật khẩu không được vượt quá 100 ký tự.");
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.EmailOrUsername).NotEmpty().WithMessage("Email hoặc tên đăng nhập là bắt buộc.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Mật khẩu là bắt buộc.");
    }
}

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("Refresh token là bắt buộc.");
    }
}

public class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{
    public LogoutRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("Refresh token là bắt buộc.");
    }
}

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email là bắt buộc.")
            .EmailAddress().WithMessage("Email không hợp lệ.")
            .MaximumLength(100).WithMessage("Email không được vượt quá 100 ký tự.");
    }
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email là bắt buộc.")
            .EmailAddress().WithMessage("Email không hợp lệ.")
            .MaximumLength(100).WithMessage("Email không được vượt quá 100 ký tự.");
        RuleFor(x => x.Otp)
            .NotEmpty().WithMessage("Mã OTP là bắt buộc.")
            .Matches("^\\d{6}$").WithMessage("Mã OTP phải gồm 6 chữ số.");
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Mật khẩu mới là bắt buộc.")
            .MinimumLength(6).WithMessage("Mật khẩu mới phải có ít nhất 6 ký tự.")
            .MaximumLength(100).WithMessage("Mật khẩu mới không được vượt quá 100 ký tự.");
    }
}
