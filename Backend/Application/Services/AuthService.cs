using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IEmailSender _emailSender;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RefreshTokenRequest> _refreshValidator;
    private readonly IValidator<LogoutRequest> _logoutValidator;
    private readonly IValidator<ForgotPasswordRequest> _forgotPasswordValidator;
    private readonly IValidator<ResetPasswordRequest> _resetPasswordValidator;
    private readonly IValidator<UpdateProfileRequest> _updateProfileValidator;

    public AuthService(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IEmailSender emailSender,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<RefreshTokenRequest> refreshValidator,
        IValidator<LogoutRequest> logoutValidator,
        IValidator<ForgotPasswordRequest> forgotPasswordValidator,
        IValidator<ResetPasswordRequest> resetPasswordValidator,
        IValidator<UpdateProfileRequest> updateProfileValidator)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _emailSender = emailSender;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _refreshValidator = refreshValidator;
        _logoutValidator = logoutValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _resetPasswordValidator = resetPasswordValidator;
        _updateProfileValidator = updateProfileValidator;
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        await _registerValidator.EnsureValidAsync(request, cancellationToken);

        var username = request.Username.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        if (_unitOfWork.Users.Query().Any(user => user.Username.ToLower() == username.ToLower()))
        {
            throw new AppException("Tên đăng nhập đã tồn tại.", 409);
        }

        if (_unitOfWork.Users.Query().Any(user => user.Email.ToLower() == email))
        {
            throw new AppException("Email đã được sử dụng.", 409);
        }

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        await _loginValidator.EnsureValidAsync(request, cancellationToken);

        var login = request.EmailOrUsername.Trim().ToLowerInvariant();
        var user = _unitOfWork.Users.Query()
            .FirstOrDefault(user => user.Email.ToLower() == login || user.Username.ToLower() == login);

        if (user is null ||
            string.IsNullOrWhiteSpace(user.PasswordHash) ||
            !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new AppException("Email/tên đăng nhập hoặc mật khẩu không đúng.", 401);
        }

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> LoginWithGoogleAsync(
        GoogleIdentity identity,
        CancellationToken cancellationToken = default)
    {
        var subject = identity.Subject.Trim();
        var email = identity.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(email))
        {
            throw new AppException("Không thể xác minh thông tin tài khoản Google.", 400);
        }

        if (email.Length > 100)
        {
            throw new AppException("Email do Google cung cấp không hợp lệ.", 400);
        }

        var user = _unitOfWork.Users.Query()
            .FirstOrDefault(item => item.GoogleSubject == subject);

        if (user is null)
        {
            user = _unitOfWork.Users.Query()
                .FirstOrDefault(item => item.Email.ToLower() == email);

            if (user is not null)
            {
                if (!string.IsNullOrWhiteSpace(user.GoogleSubject) && user.GoogleSubject != subject)
                {
                    throw new AppException("Email này đã được liên kết với một tài khoản Google khác.", 409);
                }

                user.GoogleSubject = subject;
                if (string.IsNullOrWhiteSpace(user.FullName) && !string.IsNullOrWhiteSpace(identity.FullName))
                {
                    user.FullName = NormalizeGoogleFullName(identity.FullName);
                }
            }
            else
            {
                user = new User
                {
                    Username = CreateGoogleUsername(email),
                    Email = email,
                    FullName = NormalizeGoogleFullName(identity.FullName),
                    GoogleSubject = subject,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Users.AddAsync(user, cancellationToken);
            }
        }

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshTokenAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        await _refreshValidator.EnsureValidAsync(request, cancellationToken);

        var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
        var storedToken = _unitOfWork.RefreshTokens.Query()
            .FirstOrDefault(token =>
                token.TokenHash == tokenHash &&
                token.RevokedAt == null &&
                token.ExpiresAt > DateTime.UtcNow);

        if (storedToken is null)
        {
            throw new AppException("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.", 401);
        }

        var user = await _unitOfWork.Users.GetByIdAsync(storedToken.UserId, cancellationToken)
            ?? throw new AppException("Tài khoản của phiên đăng nhập không còn tồn tại.", 401);
        EnsureUserIsActive(user);

        var refreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenHash = _tokenService.HashRefreshToken(refreshToken);
        var newStoredToken = CreateRefreshToken(user.Id, refreshTokenHash);

        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.ReplacedByTokenHash = refreshTokenHash;

        await _unitOfWork.RefreshTokens.AddAsync(newStoredToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            DtoMapper.ToDto(user),
            _tokenService.GenerateAccessToken(user),
            refreshToken,
            _tokenService.AccessTokenExpiresAt,
            newStoredToken.ExpiresAt);
    }

    public async Task LogoutAsync(
        LogoutRequest request,
        CancellationToken cancellationToken = default)
    {
        await _logoutValidator.EnsureValidAsync(request, cancellationToken);

        var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
        var storedToken = _unitOfWork.RefreshTokens.Query()
            .FirstOrDefault(token => token.TokenHash == tokenHash && token.RevokedAt == null);

        if (storedToken is null)
        {
            return;
        }

        storedToken.RevokedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        await _forgotPasswordValidator.EnsureValidAsync(request, cancellationToken);

        var email = request.Email.Trim().ToLowerInvariant();
        var user = _unitOfWork.Users.Query()
            .FirstOrDefault(user => user.Email.ToLower() == email);

        if (user is null)
        {
            throw new AppException("Email này chưa được đăng ký.", 404);
        }

        var now = DateTime.UtcNow;
        var oldOtps = _unitOfWork.PasswordResetOtps.Query()
            .Where(otp => otp.UserId == user.Id && otp.UsedAt == null && otp.ExpiresAt > now)
            .ToArray();

        foreach (var oldOtp in oldOtps)
        {
            oldOtp.UsedAt = now;
        }

        var otp = GenerateOtp();
        var resetOtp = new PasswordResetOtp
        {
            UserId = user.Id,
            OtpHash = HashOtp(user.Id, otp),
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(10)
        };

        await _unitOfWork.PasswordResetOtps.AddAsync(resetOtp, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _emailSender.SendAsync(
            user.Email,
            "TodoFlow - Mã OTP đặt lại mật khẩu",
            BuildPasswordResetEmail(user.Username, otp),
            cancellationToken);
    }

    public async Task ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        await _resetPasswordValidator.EnsureValidAsync(request, cancellationToken);

        var email = request.Email.Trim().ToLowerInvariant();
        var user = _unitOfWork.Users.Query()
            .FirstOrDefault(user => user.Email.ToLower() == email)
            ?? throw new AppException("Mã OTP không hợp lệ hoặc đã hết hạn.", 400);

        var now = DateTime.UtcNow;
        var otpHash = HashOtp(user.Id, request.Otp.Trim());
        var resetOtp = _unitOfWork.PasswordResetOtps.Query()
            .Where(otp =>
                otp.UserId == user.Id &&
                otp.UsedAt == null &&
                otp.ExpiresAt > now)
            .OrderByDescending(otp => otp.CreatedAt)
            .FirstOrDefault();

        if (resetOtp is null || resetOtp.AttemptCount >= 5 || resetOtp.OtpHash != otpHash)
        {
            if (resetOtp is not null)
            {
                resetOtp.AttemptCount++;
                if (resetOtp.AttemptCount >= 5)
                {
                    resetOtp.UsedAt = now;
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            throw new AppException("Mã OTP không hợp lệ hoặc đã hết hạn.", 400);
        }

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        resetOtp.UsedAt = now;

        var refreshTokens = _unitOfWork.RefreshTokens.Query()
            .Where(token => token.UserId == user.Id && token.RevokedAt == null)
            .ToArray();

        foreach (var refreshToken in refreshTokens)
        {
            refreshToken.RevokedAt = now;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserDto> GetMeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy người dùng.");

        return DtoMapper.ToDto(user);
    }

    public async Task<UserDto> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        await _updateProfileValidator.EnsureValidAsync(request, cancellationToken);

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy người dùng.");

        user.FullName = ProfileInputNormalizer.NormalizeOptional(request.FullName);
        user.PhoneNumber = ProfileInputNormalizer.NormalizePhoneNumber(request.PhoneNumber);
        user.DateOfBirth = request.DateOfBirth;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return DtoMapper.ToDto(user);
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        EnsureUserIsActive(user);

        var refreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenHash = _tokenService.HashRefreshToken(refreshToken);
        var storedToken = CreateRefreshToken(user.Id, refreshTokenHash);

        await _unitOfWork.RefreshTokens.AddAsync(storedToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            DtoMapper.ToDto(user),
            _tokenService.GenerateAccessToken(user),
            refreshToken,
            _tokenService.AccessTokenExpiresAt,
            storedToken.ExpiresAt);
    }

    private RefreshToken CreateRefreshToken(Guid userId, string tokenHash) =>
        new()
        {
            UserId = userId,
            TokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = _tokenService.RefreshTokenExpiresAt
        };

    private static void EnsureUserIsActive(User user)
    {
        if (!user.IsActive)
        {
            throw new AppException("Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.", 403);
        }
    }

    private string CreateGoogleUsername(string email)
    {
        var localPart = email.Split('@', 2)[0];
        var sanitized = new string(localPart
            .Select(character => char.IsLetterOrDigit(character) || character is '.' or '_' or '-' ? character : '-')
            .ToArray())
            .Trim('.', '-', '_');

        var baseUsername = string.IsNullOrWhiteSpace(sanitized) ? "google-user" : sanitized;
        baseUsername = baseUsername[..Math.Min(baseUsername.Length, 50)];

        var candidate = baseUsername;
        var suffix = 2;
        while (_unitOfWork.Users.Query().Any(user => user.Username.ToLower() == candidate.ToLower()))
        {
            var suffixText = suffix.ToString();
            var prefixLength = Math.Max(1, 50 - suffixText.Length);
            candidate = $"{baseUsername[..Math.Min(baseUsername.Length, prefixLength)]}{suffixText}";
            suffix++;
        }

        return candidate;
    }

    private static string? NormalizeGoogleFullName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var name = value.Trim();
        return name[..Math.Min(name.Length, 100)];
    }

    private static string GenerateOtp() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    private static string HashOtp(Guid userId, string otp)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{userId:N}:{otp}"));
        return Convert.ToHexString(bytes);
    }

    private static string BuildPasswordResetEmail(string username, string otp) =>
        $"""
        <div style="font-family:Arial,sans-serif;line-height:1.5;color:#111827">
          <h2>Đặt lại mật khẩu TodoFlow</h2>
          <p>Xin chào {EscapeHtml(username)},</p>
          <p>Mã OTP đặt lại mật khẩu của bạn là:</p>
          <div style="font-size:28px;font-weight:700;letter-spacing:6px;background:#f3f4f6;border-radius:8px;padding:12px 16px;display:inline-block">{otp}</div>
          <p>Mã này có hiệu lực trong 10 phút. Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này.</p>
        </div>
        """;

    private static string EscapeHtml(string value) =>
        value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&#39;", StringComparison.Ordinal);
}
