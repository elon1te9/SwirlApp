using Swirl.Api.Models;

namespace Swirl.Api.Interfaces;

public interface IPasswordHashService
{
    string HashPassword(User user, string password);

    bool VerifyPassword(User user, string password);
}
