namespace SMS.Application;

public interface ITicketService
{
    Task<PagedResult<TicketSummaryDto>> GetTicketsAsync(TicketQuery query, CancellationToken cancellationToken = default);
    Task<TicketDetailDto> GetTicketAsync(int number, CancellationToken cancellationToken = default);
    Task<TicketDetailDto> CreateTicketAsync(CreateTicketRequest request, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(int number, UpdateTicketStatusRequest request, CancellationToken cancellationToken = default);
    Task UpdatePriorityAsync(int number, UpdateTicketPriorityRequest request, CancellationToken cancellationToken = default);
    Task UpdateAssignmentAsync(int number, AssignTicketRequest request, CancellationToken cancellationToken = default);
    Task AddCommentAsync(int number, AddCommentRequest request, CancellationToken cancellationToken = default);
    Task LogTimeAsync(int number, LogTimeRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TicketActivityDto>> GetTimelineAsync(int number, CancellationToken cancellationToken = default);
}
