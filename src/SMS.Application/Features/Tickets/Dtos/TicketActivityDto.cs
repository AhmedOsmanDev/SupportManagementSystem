namespace SMS.Application;

public sealed record TicketActivityDto(
    Guid Id,
    string Type,
    string Description,
    string? PerformedBy,
    string? OldValue,
    string? NewValue,
    DateTime CreatedAt);
