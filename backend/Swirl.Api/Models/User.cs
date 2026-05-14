namespace Swirl.Api.Models;

public class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public UserProfile? UserProfile { get; set; }

    public ICollection<UserLevelProgress> UserLevelProgresses { get; set; } = new List<UserLevelProgress>();

    public ICollection<UserWordProgress> UserWordProgresses { get; set; } = new List<UserWordProgress>();

    public ICollection<LevelAttempt> LevelAttempts { get; set; } = new List<LevelAttempt>();

    public ICollection<DailyTest> DailyTests { get; set; } = new List<DailyTest>();
}
