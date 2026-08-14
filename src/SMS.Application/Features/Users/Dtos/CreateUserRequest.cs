using System.ComponentModel.DataAnnotations;
using SMS.Domain;

namespace SMS.Application;

public sealed class CreateUserRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string FirstName { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    public string LastName { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;

    [EnumDataType(typeof(UserRole))]
    public UserRole Role { get; init; }
}
