using FluentValidation;
using TodoApp.Application.Common;
using TodoApp.Application.DTOs;
using TodoApp.Application.Interfaces;
using TodoApp.Domain.Entities;

namespace TodoApp.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RefreshTokenRequest> _refreshValidator;
    private readonly IValidator<LogoutRequest> _logoutValidator;

    public AuthService(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<RefreshTokenRequest> refreshValidator,
        IValidator<LogoutRequest> logoutValidator)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _refreshValidator = refreshValidator;
        _logoutValidator = logoutValidator;
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

        if (user is null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new AppException("Email/tên đăng nhập hoặc mật khẩu không đúng.", 401);
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

    public async Task<UserDto> GetMeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy người dùng.");

        return DtoMapper.ToDto(user);
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
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
}
