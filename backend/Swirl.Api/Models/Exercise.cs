namespace Swirl.Api.Models;

public class Exercise
{
    public int Id { get; set; }

    public int LevelId { get; set; }

    public int WordId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string? QuestionText { get; set; }

    public string CorrectAnswer { get; set; } = string.Empty;

    public int? SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Level Level { get; set; } = null!;

    public Word Word { get; set; } = null!;

    public ICollection<ExerciseOption> ExerciseOptions { get; set; } = new List<ExerciseOption>();

    public ICollection<UserAnswer> UserAnswers { get; set; } = new List<UserAnswer>();
}
