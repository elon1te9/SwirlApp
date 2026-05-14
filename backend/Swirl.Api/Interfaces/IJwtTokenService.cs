using Swirl.Api.Models;

namespace Swirl.Api.Interfaces;

public interface IJwtTokenService
{
    string CreateAccessToken(User user);
}
