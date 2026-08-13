using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SMS.Application;
using SMS.Domain;
using SMS.Infrastructure.Persistence;

namespace SMS.Infrastructure;

public sealed class AuthService(
    ApplicationDbContext dbContext,
    ICurrentUser currentUser,
    ITokenGenerator tokenGenerator,
    IPasswordHasher<User> passwordHasher) : IAuthService
{
    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users.SingleOrDefaultAsync(candidate => candidate.Email == normalizedEmail, cancellationToken);
        if (user is null || !user.IsActive ||
            passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Invalid email or password.");

        var token = tokenGenerator.Create(user);
        return new AuthResponse(token.AccessToken, token.ExpiresAt, Map(user));
    }

    public async Task<UserDto> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated)
            throw new UnauthorizedAccessException("Authentication is required.");

        var user = await dbContext.Users.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == currentUser.UserId && candidate.IsActive, cancellationToken)
            ?? throw new UnauthorizedAccessException("The authenticated user is unavailable.");
        return Map(user);
    }

    private static UserDto Map(User user) => new(user.Id, user.FirstName, user.LastName, user.Email, user.Role);
}
