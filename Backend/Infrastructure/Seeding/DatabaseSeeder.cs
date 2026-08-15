using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;

namespace Infrastructure.Seeding;

public class DatabaseSeeder
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly AdminBootstrapOptions _adminOptions;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        AppDbContext dbContext,
        IPasswordHasher passwordHasher,
        IOptions<AdminBootstrapOptions> adminOptions,
        ILogger<DatabaseSeeder> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _adminOptions = adminOptions.Value;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!_adminOptions.HasEmail)
        {
            _logger.LogInformation("Admin bootstrap skipped because AdminBootstrap:Email is not configured.");
            return;
        }

        var email = _adminOptions.Email.Trim().ToLowerInvariant();
        var admin = await _dbContext.Users
            .FirstOrDefaultAsync(user => user.Email == email, cancellationToken);

        if (admin is null)
        {
            if (!_adminOptions.CanCreateAccount)
            {
                _logger.LogWarning(
                    "Admin bootstrap cannot create {Email}. Configure AdminBootstrap:Username and AdminBootstrap:Password or register this email first.",
                    email);
                return;
            }

            var username = _adminOptions.Username.Trim();
            if (await _dbContext.Users.AnyAsync(user => user.Username == username, cancellationToken))
            {
                _logger.LogWarning("Admin bootstrap cannot create {Email} because username {Username} is already used.", email, username);
                return;
            }

            admin = new User
            {
                Username = username,
                Email = email,
                PasswordHash = _passwordHasher.HashPassword(_adminOptions.Password),
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _dbContext.Users.AddAsync(admin, cancellationToken);
        }
        else
        {
            admin.Role = UserRole.Admin;
            admin.IsActive = true;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
