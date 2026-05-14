namespace Swirl.Api.Responses;

public class LevelDetailsResponse : LevelResponse
{
    public string SectionTitle { get; set; } = string.Empty;

    public bool WordsLearned { get; set; }
}
