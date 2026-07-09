using TodoApp.Application.Interfaces;

namespace TodoApp.Application.Services;

public class NoopEmailSender : IEmailSender
{
    public Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
