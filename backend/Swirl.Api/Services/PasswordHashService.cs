using Microsoft.AspNetCore.Identity;
using Swirl.Api.Interfaces;
using Swirl.Api.Models;

namespace Swirl.Api.Services;

public class PasswordHashService : IPasswordHashService
{
    private readonly PasswordHasher<User> _passwordHasher = new PasswordHasher<User>();

    public string HashPassword(User user, string password)
    {
        return _passwordHasher.HashPassword(user, password);
    }

    public bool VerifyPassword(User user, string password)
    {
        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
