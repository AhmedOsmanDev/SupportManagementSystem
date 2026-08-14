namespace SMS.Application;

public sealed record DashboardDto(
    int TotalTickets,
    int OpenTickets,
    int InProgressTickets,
    int ResolvedTickets,
    int ClosedTickets,
    int OpenCriticalTickets,
    double AverageResolutionHours,
    IReadOnlyCollection<AgentWorkloadDto> AgentWorkload);
