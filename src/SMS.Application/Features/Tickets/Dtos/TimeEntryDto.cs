namespace SMS.Application;

public sealed record TimeEntryDto(
    Guid Id,
    Guid AgentId,
    string AgentName,
    DateTime WorkDate,
    int DurationMinutes,
    string Description,
    DateTime CreatedAt);
