namespace SMS.Domain;

public sealed class RefreshToken
{
    private RefreshToken() { }

    private RefreshToken(Guid id, Guid userId, string tokenHash, DateTime expiresAt)
    {
        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;
    public string? ReplacedByTokenHash { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    public string? RevocationReason { get; private set; }

    public User? User { get; private set; }

    public bool IsActive => !RevokedAt.HasValue && ExpiresAt > DateTime.UtcNow;

    public static RefreshToken Create(Guid userId, string tokenHash, DateTime expiresAt) =>
        new(Guid.NewGuid(), userId, tokenHash, expiresAt);

    public void Revoke(string? replacedByTokenHash = null, string? reason = null)
    {
        if (RevokedAt.HasValue)
            return;

        RevokedAt = DateTime.UtcNow;
        ReplacedByTokenHash = replacedByTokenHash;
        RevocationReason = reason;
    }
}