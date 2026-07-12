using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Application.Interfaces;
using Infrastructure.Auth;
using Infrastructure.Data;
using Infrastructure.Email;
using Infrastructure.Seeding;
using Infrastructure.UnitOfWork;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost,1433;Database=TodoFlowDb;User Id=sa;Password=Admin123A@;TrustServerCertificate=True;MultipleActiveResultSets=True";

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));

        services.AddScoped<IUnitOfWork, global::Infrastructure.UnitOfWork.UnitOfWork>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}
