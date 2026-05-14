namespace Swirl.Api.Models;

public class UserProfile
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int AvatarId { get; set; }

    public int CurrentStreak { get; set; }

    public int BestStreak { get; set; }

    public DateOnly? LastActivityDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public User User { get; set; } = null!;

    public Avatar Avatar { get; set; } = null!;
}
