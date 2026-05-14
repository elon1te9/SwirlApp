namespace Swirl.Api.Models;

public class Word
{
    public int Id { get; set; }

    public int LevelId { get; set; }

    public string English { get; set; } = string.Empty;

    public string Russian { get; set; } = string.Empty;

    public string? Transcription { get; set; }

    public string? PartOfSpeech { get; set; }

    public string? ImageUrl { get; set; }

    public string? AudioUrl { get; set; }

    public string CefrLevel { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Level Level { get; set; } = null!;

    public ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();

    public ICollection<UserWordProgress> UserWordProgresses { get; set; } = new List<UserWordProgress>();

    public ICollection<DailyTestAnswer> DailyTestAnswers { get; set; } = new List<DailyTestAnswer>();
}
