namespace TodoApp.Domain.Entities;

public class PasswordResetOtp
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string OtpHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public int AttemptCount { get; set; }

    public User? User { get; set; }
}
