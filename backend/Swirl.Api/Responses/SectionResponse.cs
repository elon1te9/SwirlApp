namespace Swirl.Api.Responses;

public class SectionResponse
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public int ProgressPercent { get; set; }

    public int CompletedLevels { get; set; }

    public int TotalLevels { get; set; }
}
