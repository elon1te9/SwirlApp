namespace Swirl.Api.Models;

public class UserAnswer
{
    public int Id { get; set; }

    public int AttemptId { get; set; }

    public int ExerciseId { get; set; }

    public string UserAnswerText { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }

    public DateTime AnsweredAt { get; set; }

    public int? TimeSpentMs { get; set; }

    public LevelAttempt Attempt { get; set; } = null!;

    public Exercise Exercise { get; set; } = null!;
}
