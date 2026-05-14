using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Swirl.Api.Interfaces;
using Swirl.Api.Models;

namespace Swirl.Api.Services;

public class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    public string CreateAccessToken(User user)
    {
        var secret = configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("JWT secret is not configured.");
        }

        var issuer = configuration["Jwt:Issuer"] ?? "Swirl.Api";
        var audience = configuration["Jwt:Audience"] ?? "Swirl.Android";
        var accessTokenMinutes = configuration.GetValue("Jwt:AccessTokenMinutes", 60);
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email)
        };

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(accessTokenMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
