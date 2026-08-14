namespace SMS.Application;

public sealed record TokenSettings(
    string Secret,
    string Issuer,
    string Audience,
    int AccessTokenMinutes,
    int RefreshTokenDays);