using System.ComponentModel.DataAnnotations;

namespace SMS.Application;

public sealed class RefreshTokenRequest
{
    [Required, StringLength(200, MinimumLength = 32)]
    public string RefreshToken { get; init; } = string.Empty;
}