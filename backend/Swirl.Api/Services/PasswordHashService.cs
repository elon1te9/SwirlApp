using Microsoft.AspNetCore.Identity;
using Swirl.Api.Interfaces;
using Swirl.Api.Models;

namespace Swirl.Api.Services;

public class PasswordHashService : IPasswordHashService
{
    private readonly PasswordHasher<User> passwordHasher = new();

    public string HashPassword(User user, string password) =>
        passwordHasher.HashPassword(user, password);

    public bool VerifyPassword(User user, string password)
    {
        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
