namespace Swirl.Api.Models;

public class ExerciseOption
{
    public int Id { get; set; }

    public int ExerciseId { get; set; }

    public string OptionText { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }

    public int? SortOrder { get; set; }

    public Exercise Exercise { get; set; } = null!;
}
