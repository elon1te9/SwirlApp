using System.ComponentModel.DataAnnotations;

namespace Swirl.Api.Requests;

public class RegisterRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(Password), ErrorMessage = "Password and confirmPassword must match")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Avatar is required")]
    public int AvatarId { get; set; }
}
