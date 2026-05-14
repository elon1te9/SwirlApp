namespace Swirl.Api.Responses;

public class WordResponse
{
    public int Id { get; set; }

    public string English { get; set; } = string.Empty;

    public string Russian { get; set; } = string.Empty;

    public string? Transcription { get; set; }

    public string? PartOfSpeech { get; set; }

    public string? ImageUrl { get; set; }

    public string? AudioUrl { get; set; }
}
