using Domain.Entities;

namespace Application.Interfaces;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}

public interface ITokenService
{
    DateTime AccessTokenExpiresAt { get; }
    DateTime RefreshTokenExpiresAt { get; }
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);
}
