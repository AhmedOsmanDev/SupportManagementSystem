namespace SMS.Application;

public sealed record TokenSubject(
    Guid Id,
    string FullName,
    string FirstName,
    string LastName,
    string Email,
    string Role);
