using SMS.Domain;

namespace SMS.Application;

public sealed record TicketDetailDto(
    int Number,
    string Title,
    string Description,
    TicketStatus Status,
    TicketPriority Priority,
    Guid CustomerId,
    string CustomerName,
    Guid? AssignedAgentId,
    string? AssignedAgentName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int TotalTimeMinutes,
    DateTime? ResolvedAt,
    DateTime? ClosedAt,
    IReadOnlyCollection<CommentDto> Comments,
    IReadOnlyCollection<TicketActivityDto> Activities,
    IReadOnlyCollection<TimeEntryDto> TimeEntries);
