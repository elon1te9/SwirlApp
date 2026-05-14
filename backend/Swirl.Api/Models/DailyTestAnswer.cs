namespace Swirl.Api.Models;

public class DailyTestAnswer
{
    public int Id { get; set; }

    public int DailyTestId { get; set; }

    public int WordId { get; set; }

    public string ExerciseType { get; set; } = string.Empty;

    public string UserAnswerText { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }

    public DateTime AnsweredAt { get; set; }

    public DailyTest DailyTest { get; set; } = null!;

    public Word Word { get; set; } = null!;
}
