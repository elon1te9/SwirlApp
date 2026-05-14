namespace Swirl.Api.Models;

public class LevelAttempt
{
    public int Id { get; set; }

    public Guid UserId { get; set; }

    public int LevelId { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public int MistakesCount { get; set; }

    public bool IsSuccessful { get; set; }

    public User User { get; set; } = null!;

    public Level Level { get; set; } = null!;

    public ICollection<UserAnswer> UserAnswers { get; set; } = new List<UserAnswer>();
}
