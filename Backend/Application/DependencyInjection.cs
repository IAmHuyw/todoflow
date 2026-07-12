using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Application.Interfaces;
using Application.Services;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<ITaskShareService, TaskShareService>();
        services.AddScoped<ISubTaskService, SubTaskService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IReminderService, ReminderService>();
        services.AddScoped<IRealtimeNotifier, NoopRealtimeNotifier>();
        services.AddScoped<IEmailSender, NoopEmailSender>();

        return services;
    }
}
