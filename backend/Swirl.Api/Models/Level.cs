namespace Swirl.Api.Models;

public class Level
{
    public int Id { get; set; }

    public int SectionId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int LevelNumber { get; set; }

    public string CefrLevel { get; set; } = string.Empty;

    public bool IsFinalTest { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Section Section { get; set; } = null!;

    public ICollection<Word> Words { get; set; } = new List<Word>();

    public ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();

    public ICollection<UserLevelProgress> UserLevelProgresses { get; set; } = new List<UserLevelProgress>();

    public ICollection<LevelAttempt> LevelAttempts { get; set; } = new List<LevelAttempt>();
}
