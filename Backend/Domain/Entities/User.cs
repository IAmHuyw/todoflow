using Domain.Enums;

namespace Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    // Null for accounts created through Google before they choose a local password.
    public string? PasswordHash { get; set; }
    public string? GoogleSubject { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<TodoTask> Tasks { get; set; } = new List<TodoTask>();
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<PasswordResetOtp> PasswordResetOtps { get; set; } = new List<PasswordResetOtp>();
    public ICollection<TaskShare> OwnedTaskShares { get; set; } = new List<TaskShare>();
    public ICollection<TaskShare> ReceivedTaskShares { get; set; } = new List<TaskShare>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<TodoTask> AssignedTasks { get; set; } = new List<TodoTask>();
    public ICollection<TaskComment> TaskComments { get; set; } = new List<TaskComment>();
    public ICollection<TaskActivity> TaskActivities { get; set; } = new List<TaskActivity>();
}
