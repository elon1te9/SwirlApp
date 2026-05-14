namespace Swirl.Api.Models;

public class Avatar
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<UserProfile> UserProfiles { get; set; } = new List<UserProfile>();
}
