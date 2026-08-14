using SMS.Domain;

namespace SMS.Application;

public record TicketSummaryDto(
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
    int TotalTimeMinutes);
