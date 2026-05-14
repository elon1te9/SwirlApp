namespace Swirl.Api.Responses;

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public CurrentUserResponse User { get; set; } = new();
}
