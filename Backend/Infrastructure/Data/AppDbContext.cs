using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetOtp> PasswordResetOtps => Set<PasswordResetOtp>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<TodoTask> Tasks => Set<TodoTask>();
    public DbSet<SubTask> SubTasks => Set<SubTask>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<TaskTag> TaskTags => Set<TaskTag>();
    public DbSet<TaskShare> TaskShares => Set<TaskShare>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<TaskReminder> TaskReminders => Set<TaskReminder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Username).HasMaxLength(50).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(100).IsRequired();
            entity.Property(user => user.FullName).HasMaxLength(100);
            entity.Property(user => user.PhoneNumber).HasMaxLength(16);
            entity.Property(user => user.DateOfBirth).HasColumnType("date");
            entity.Property(user => user.PasswordHash).HasMaxLength(255).IsRequired();
            entity.HasIndex(user => user.Username).IsUnique();
            entity.HasIndex(user => user.Email).IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(token => token.Id);
            entity.Property(token => token.TokenHash).HasMaxLength(128).IsRequired();
            entity.HasIndex(token => token.TokenHash).IsUnique();
            entity.HasOne(token => token.User)
                .WithMany(user => user.RefreshTokens)
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PasswordResetOtp>(entity =>
        {
            entity.ToTable("PasswordResetOtps");
            entity.HasKey(otp => otp.Id);
            entity.Property(otp => otp.OtpHash).HasMaxLength(128).IsRequired();
            entity.HasIndex(otp => new { otp.UserId, otp.ExpiresAt });
            entity.HasOne(otp => otp.User)
                .WithMany(user => user.PasswordResetOtps)
                .HasForeignKey(otp => otp.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(category => category.Id);
            entity.Property(category => category.Name).HasMaxLength(50).IsRequired();
            entity.Property(category => category.Color).HasMaxLength(20).IsRequired();
            entity.HasIndex(category => new { category.UserId, category.Name }).IsUnique();
            entity.HasOne(category => category.User)
                .WithMany(user => user.Categories)
                .HasForeignKey(category => category.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<TodoTask>(entity =>
        {
            entity.ToTable("Tasks");
            entity.HasKey(task => task.Id);
            entity.Property(task => task.Title).HasMaxLength(200).IsRequired();
            entity.Property(task => task.Description).HasMaxLength(1000);
            entity.Property(task => task.RecurrenceType).HasConversion<int>();
            entity.Property(task => task.RecurrenceInterval).HasDefaultValue(1);
            entity.HasQueryFilter(task => !task.IsDeleted);
            entity.HasOne(task => task.User)
                .WithMany(user => user.Tasks)
                .HasForeignKey(task => task.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(task => task.Category)
                .WithMany(category => category.Tasks)
                .HasForeignKey(task => task.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(task => new { task.UserId, task.Status });
            entity.HasIndex(task => new { task.UserId, task.Status, task.SortOrder });
            entity.HasIndex(task => new { task.UserId, task.Priority });
            entity.HasIndex(task => new { task.UserId, task.DueDate });
        });

        modelBuilder.Entity<SubTask>(entity =>
        {
            entity.ToTable("SubTasks");
            entity.HasKey(subTask => subTask.Id);
            entity.Property(subTask => subTask.Title).HasMaxLength(200).IsRequired();
            entity.Property(subTask => subTask.Note).HasMaxLength(1000);
            entity.HasQueryFilter(subTask => !subTask.Task.IsDeleted);
            entity.HasOne(subTask => subTask.Task)
                .WithMany(task => task.SubTasks)
                .HasForeignKey(subTask => subTask.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.ToTable("Tags");
            entity.HasKey(tag => tag.Id);
            entity.Property(tag => tag.Name).HasMaxLength(50).IsRequired();
            entity.HasIndex(tag => new { tag.UserId, tag.Name }).IsUnique();
            entity.HasOne(tag => tag.User)
                .WithMany(user => user.Tags)
                .HasForeignKey(tag => tag.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<TaskTag>(entity =>
        {
            entity.ToTable("TaskTags");
            entity.HasKey(taskTag => new { taskTag.TaskId, taskTag.TagId });
            entity.HasQueryFilter(taskTag => !taskTag.Task.IsDeleted);
            entity.HasOne(taskTag => taskTag.Task)
                .WithMany(task => task.TaskTags)
                .HasForeignKey(taskTag => taskTag.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(taskTag => taskTag.Tag)
                .WithMany(tag => tag.TaskTags)
                .HasForeignKey(taskTag => taskTag.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskShare>(entity =>
        {
            entity.ToTable("TaskShares");
            entity.HasKey(share => share.Id);
            entity.HasQueryFilter(share => !share.Task.IsDeleted);
            entity.HasIndex(share => new { share.TaskId, share.SharedWithUserId }).IsUnique();
            entity.HasIndex(share => new { share.SharedWithUserId, share.Status });
            entity.HasOne(share => share.Task)
                .WithMany(task => task.Shares)
                .HasForeignKey(share => share.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(share => share.Owner)
                .WithMany(user => user.OwnedTaskShares)
                .HasForeignKey(share => share.OwnerId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(share => share.SharedWithUser)
                .WithMany(user => user.ReceivedTaskShares)
                .HasForeignKey(share => share.SharedWithUserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(notification => notification.Id);
            entity.Property(notification => notification.Message).HasMaxLength(500).IsRequired();
            entity.HasIndex(notification => new { notification.UserId, notification.IsRead, notification.CreatedAt });
            entity.HasOne(notification => notification.User)
                .WithMany(user => user.Notifications)
                .HasForeignKey(notification => notification.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(notification => notification.Task)
                .WithMany(task => task.Notifications)
                .HasForeignKey(notification => notification.TaskId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<TaskReminder>(entity =>
        {
            entity.ToTable("TaskReminders");
            entity.HasKey(reminder => reminder.Id);
            entity.HasQueryFilter(reminder => !reminder.Task.IsDeleted);
            entity.HasIndex(reminder => new { reminder.IsSent, reminder.RemindAt });
            entity.HasOne(reminder => reminder.Task)
                .WithMany(task => task.Reminders)
                .HasForeignKey(reminder => reminder.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
