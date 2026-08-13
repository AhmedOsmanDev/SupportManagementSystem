using Microsoft.EntityFrameworkCore;
using SMS.Application;
using SMS.Domain;
using SMS.Infrastructure.Persistence;

namespace SMS.Infrastructure;

public sealed class DashboardService(ApplicationDbContext dbContext, ICurrentUser currentUser) : IDashboardService
{
    public async Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        EnsureAdmin();
        var tickets = dbContext.Tickets.AsNoTracking();
        var total = await tickets.CountAsync(cancellationToken);
        var open = await tickets.CountAsync(ticket => ticket.Status == TicketStatus.Open, cancellationToken);
        var inProgress = await tickets.CountAsync(ticket => ticket.Status == TicketStatus.InProgress, cancellationToken);
        var resolved = await tickets.CountAsync(ticket => ticket.Status == TicketStatus.Resolved, cancellationToken);
        var closed = await tickets.CountAsync(ticket => ticket.Status == TicketStatus.Closed, cancellationToken);
        var openCritical = await tickets.CountAsync(ticket =>
            ticket.Priority == TicketPriority.Critical &&
            (ticket.Status == TicketStatus.Open || ticket.Status == TicketStatus.InProgress), cancellationToken);

        var resolutionDates = await tickets
            .Where(ticket => ticket.ResolvedAt.HasValue)
            .Select(ticket => new { ticket.CreatedAt, ResolvedAt = ticket.ResolvedAt!.Value })
            .ToListAsync(cancellationToken);
        var averageHours = resolutionDates.Count == 0
            ? 0
            : resolutionDates.Average(ticket => (ticket.ResolvedAt - ticket.CreatedAt).TotalHours);

        var workload = await dbContext.Users.AsNoTracking()
            .Where(user => user.Role == UserRole.SupportAgent && user.IsActive)
            .OrderBy(user => user.FirstName).ThenBy(user => user.LastName)
            .Select(user => new AgentWorkloadDto(
                user.Id,
                user.FirstName + " " + user.LastName,
                dbContext.Tickets.Count(ticket => ticket.AssignedSupportId == user.Id &&
                    ticket.Status != TicketStatus.Resolved && ticket.Status != TicketStatus.Closed),
                dbContext.TimeEntries.Where(entry => entry.AgentId == user.Id).Sum(entry => (int?)entry.DurationMinutes) ?? 0))
            .ToListAsync(cancellationToken);

        return new DashboardDto(total, open, inProgress, resolved, closed, openCritical,
            Math.Round(averageHours, 2), workload);
    }

    private void EnsureAdmin()
    {
        if (!currentUser.IsAuthenticated)
            throw new UnauthorizedAccessException("Authentication is required.");
        if (currentUser.Role != UserRole.Admin)
            throw new ForbiddenException();
    }
}
