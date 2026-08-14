using SMS.Domain;

namespace SMS.Application;

public record UserDto(Guid Id, string FirstName, string LastName, string Email, UserRole Role);
