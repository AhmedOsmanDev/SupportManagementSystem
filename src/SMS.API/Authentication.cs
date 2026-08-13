using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SMS.Application;
using SMS.Domain;

namespace SMS.API;

public sealed record JwtSettings(string Secret, string Issuer, string Audience, int AccessTokenMinutes);

public sealed class JwtTokenGenerator(JwtSettings settings) : ITokenGenerator
{
    public TokenResult Create(User user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(settings.AccessTokenMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.GivenName, user.FirstName),
            new Claim(ClaimTypes.Surname, user.LastName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience, claims,
            expires: expiresAt, signingCredentials: credentials);
        return new TokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}

public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal Principal => accessor.HttpContext?.User ?? new ClaimsPrincipal();

    public Guid UserId => Guid.TryParse(Principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;
    public string Email => Principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
    public UserRole Role => Enum.TryParse<UserRole>(Principal.FindFirstValue(ClaimTypes.Role), out var role) ? role : default;
    public bool IsAuthenticated => Principal.Identity?.IsAuthenticated == true && UserId != Guid.Empty;
}
