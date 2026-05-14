namespace Swirl.Api.Models;

public class DailyTest
{
    public int Id { get; set; }

    public Guid UserId { get; set; }

    public DateOnly TestDate { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public int TotalQuestions { get; set; }

    public int CorrectAnswers { get; set; }

    public bool IsCompleted { get; set; }

    public User User { get; set; } = null!;

    public ICollection<DailyTestAnswer> DailyTestAnswers { get; set; } = new List<DailyTestAnswer>();
}
