using System.Security.Cryptography;
using System.Text;
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
    IPasswordHasher<User> passwordHasher,
    TokenSettings tokenSettings) : IAuthService
{
    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users.SingleOrDefaultAsync(candidate => candidate.Email == normalizedEmail, cancellationToken);
        if (user is null || !user.IsActive ||
            passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Invalid email or password.");

        return await IssueTokensAsync(user, null, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var refreshToken = await FindRefreshTokenAsync(request.RefreshToken, includeUser: true, cancellationToken)
            ?? throw new UnauthorizedAccessException("The refresh token is invalid.");

        if (refreshToken.RevokedAt.HasValue)
        {
            if (!string.IsNullOrWhiteSpace(refreshToken.ReplacedByTokenHash))
                await RevokeDescendantTokensAsync(refreshToken.ReplacedByTokenHash, "Refresh token reuse detected.", cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAccessException("The refresh token is invalid.");
        }

        if (refreshToken.ExpiresAt <= DateTime.UtcNow)
        {
            refreshToken.Revoke(reason: "Refresh token expired.");
            await dbContext.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAccessException("The refresh token has expired.");
        }

        var user = refreshToken.User;
        if (user is null || !user.IsActive)
        {
            refreshToken.Revoke(reason: "The user account is inactive.");
            await dbContext.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAccessException("The refresh token is invalid.");
        }

        return await IssueTokensAsync(user, refreshToken, cancellationToken);
    }

    public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var refreshToken = await FindRefreshTokenAsync(request.RefreshToken, includeUser: false, cancellationToken);
        if (refreshToken is null || refreshToken.RevokedAt.HasValue)
            return;

        refreshToken.Revoke(reason: "User signed out.");
        await dbContext.SaveChangesAsync(cancellationToken);
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

    private async Task<AuthResponse> IssueTokensAsync(
        User user,
        RefreshToken? currentRefreshToken,
        CancellationToken cancellationToken)
    {
        var accessToken = tokenGenerator.Create(new TokenSubject(
            user.Id,
            user.FullName,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Role.ToString()));
        var (rawRefreshToken, persistedRefreshToken) = CreateRefreshToken(user.Id);

        if (currentRefreshToken is not null)
            currentRefreshToken.Revoke(persistedRefreshToken.TokenHash, "Refresh token rotated.");

        dbContext.RefreshTokens.Add(persistedRefreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            accessToken.AccessToken,
            rawRefreshToken,
            accessToken.ExpiresAt,
            persistedRefreshToken.ExpiresAt,
            Map(user));
    }

    private (string RawToken, RefreshToken PersistedToken) CreateRefreshToken(Guid userId)
    {
        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));
        var expiresAt = DateTime.UtcNow.AddDays(tokenSettings.RefreshTokenDays);
        return (rawToken, RefreshToken.Create(userId, HashRefreshToken(rawToken), expiresAt));
    }

    private async Task<RefreshToken?> FindRefreshTokenAsync(
        string rawToken,
        bool includeUser,
        CancellationToken cancellationToken)
    {
        var normalizedToken = rawToken.Trim();
        if (string.IsNullOrWhiteSpace(normalizedToken))
            return null;

        IQueryable<RefreshToken> query = dbContext.RefreshTokens;
        if (includeUser)
            query = query.Include(token => token.User);

        var tokenHash = HashRefreshToken(normalizedToken);
        return await query.SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
    }

    private async Task RevokeDescendantTokensAsync(
        string replacedByTokenHash,
        string reason,
        CancellationToken cancellationToken)
    {
        var nextTokenHash = replacedByTokenHash;
        while (!string.IsNullOrWhiteSpace(nextTokenHash))
        {
            var descendant = await dbContext.RefreshTokens
                .SingleOrDefaultAsync(token => token.TokenHash == nextTokenHash, cancellationToken);
            if (descendant is null)
                break;

            var childTokenHash = descendant.ReplacedByTokenHash;
            if (!descendant.RevokedAt.HasValue)
                descendant.Revoke(reason: reason);

            nextTokenHash = childTokenHash;
        }
    }

    private static string HashRefreshToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static UserDto Map(User user) => new(user.Id, user.FirstName, user.LastName, user.Email, user.Role);
}