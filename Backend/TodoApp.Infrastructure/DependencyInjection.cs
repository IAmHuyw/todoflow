using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Application.Interfaces;
using TodoApp.Infrastructure.Auth;
using TodoApp.Infrastructure.Data;
using TodoApp.Infrastructure.Email;
using TodoApp.Infrastructure.Seeding;
using TodoApp.Infrastructure.UnitOfWork;

namespace TodoApp.Infrastructure;

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

        services.AddScoped<IUnitOfWork, global::TodoApp.Infrastructure.UnitOfWork.UnitOfWork>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}
