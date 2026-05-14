namespace Swirl.Api.Models;

public class UserWordProgress
{
    public int Id { get; set; }

    public Guid UserId { get; set; }

    public int WordId { get; set; }

    public DateTime LearnedAt { get; set; }

    public User User { get; set; } = null!;

    public Word Word { get; set; } = null!;
}
