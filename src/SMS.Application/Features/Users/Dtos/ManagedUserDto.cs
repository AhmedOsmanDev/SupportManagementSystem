using SMS.Domain;

namespace SMS.Application;

public sealed record ManagedUserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt,
    int AssignedTicketCount);
