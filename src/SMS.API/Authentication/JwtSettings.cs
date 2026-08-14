namespace SMS.API;

public sealed record JwtSettings(
    string Secret,
    string Issuer,
    string Audience,
    int AccessTokenMinutes);
