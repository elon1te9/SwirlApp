namespace Swirl.Api.Responses;

public class MarkLevelWordsLearnedResponse
{
    public int LevelId { get; set; }

    public bool WordsLearned { get; set; }

    public int LearnedWordsCount { get; set; }
}
