using System.Security.Claims;
using SMS.Application;

namespace SMS.API;

public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal Principal => accessor.HttpContext?.User ?? new ClaimsPrincipal();

    public Guid UserId => Guid.TryParse(Principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id
        : Guid.Empty;

    public string Email => Principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

    public string Role => Principal.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    public bool IsAuthenticated => Principal.Identity?.IsAuthenticated == true && UserId != Guid.Empty;
}
