using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SMS.Application;

namespace SMS.API;

public sealed class JwtTokenGenerator(TokenSettings settings) : ITokenGenerator
{
    public TokenResult Create(TokenSubject subject)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(settings.AccessTokenMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, subject.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, subject.Id.ToString()),
            new Claim(ClaimTypes.Name, subject.FullName),
            new Claim(ClaimTypes.Email, subject.Email),
            new Claim(ClaimTypes.GivenName, subject.FirstName),
            new Claim(ClaimTypes.Surname, subject.LastName),
            new Claim(ClaimTypes.Role, subject.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            settings.Issuer,
            settings.Audience,
            claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new TokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}