namespace Swirl.Api.Models;

public class UserLevelProgress
{
    public int Id { get; set; }

    public Guid UserId { get; set; }

    public int LevelId { get; set; }

    public string Status { get; set; } = string.Empty;

    public bool WordsLearned { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? UnlockedAt { get; set; }

    public int AttemptsCount { get; set; }

    public User User { get; set; } = null!;

    public Level Level { get; set; } = null!;
}
