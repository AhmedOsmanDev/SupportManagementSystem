namespace SMS.Application;

public sealed record AgentWorkloadDto(Guid AgentId, string AgentName, int ActiveTickets, int TotalMinutes);
