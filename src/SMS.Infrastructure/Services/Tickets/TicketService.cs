using Microsoft.EntityFrameworkCore;
using SMS.Application;
using SMS.Domain;
using SMS.Infrastructure.Persistence;

namespace SMS.Infrastructure;

public sealed class TicketService(
    ApplicationDbContext dbContext,
    ICurrentUser currentUser,
    ITicketNumberGenerator ticketNumberGenerator) : ITicketService
{
    public async Task<PagedResult<TicketSummaryDto>> GetTicketsAsync(TicketQuery query, CancellationToken cancellationToken = default)
    {
        var tickets = Scope(dbContext.Tickets.AsNoTracking());

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            tickets = int.TryParse(search, out var number)
                ? tickets.Where(ticket => ticket.Number == number ||
                    ticket.Title.Contains(search) || ticket.Description.Contains(search))
                : tickets.Where(ticket => ticket.Title.Contains(search) || ticket.Description.Contains(search));
        }
        if (query.Status.HasValue)
            tickets = tickets.Where(ticket => ticket.Status == query.Status.Value);
        if (query.Priority.HasValue)
            tickets = tickets.Where(ticket => ticket.Priority == query.Priority.Value);

        var totalCount = await tickets.CountAsync(cancellationToken);
        tickets = ApplySort(tickets, query.SortBy, query.SortDirection == "asc");

        var items = await tickets
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(ticket => new TicketSummaryDto(
                ticket.Number,
                ticket.Title,
                ticket.Description,
                ticket.Status,
                ticket.Priority,
                ticket.CustomerId,
                ticket.Customer!.FirstName + " " + ticket.Customer.LastName,
                ticket.AssignedSupportId,
                ticket.AssignedSupport == null ? null : ticket.AssignedSupport.FirstName + " " + ticket.AssignedSupport.LastName,
                ticket.CreatedAt,
                ticket.UpdatedAt,
                ticket.TimeEntries.Sum(entry => (int?)entry.DurationMinutes) ?? 0))
            .ToListAsync(cancellationToken);

        return new PagedResult<TicketSummaryDto>(items, query.Page, query.PageSize, totalCount,
            (int)Math.Ceiling(totalCount / (double)query.PageSize));
    }

    public async Task<TicketDetailDto> GetTicketAsync(int number, CancellationToken cancellationToken = default) =>
        MapDetail(await GetAccessibleTicketAsync(number, cancellationToken));

    public async Task<TicketDetailDto> CreateTicketAsync(CreateTicketRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        if (currentUser.Role != UserRoleNames.Customer)
            throw new ForbiddenException("Only customers can create support tickets.");

        var number = await ticketNumberGenerator.GetNextAsync(cancellationToken);
        var ticket = Ticket.Create(number, request.Title, request.Description, request.Priority, currentUser.UserId);
        dbContext.Tickets.Add(ticket);
        dbContext.TicketActivities.Add(TicketActivity.Create(number, currentUser.UserId, "Created", "Ticket created."));
        await SaveChangesAsync(cancellationToken);
        return MapDetail(await GetAccessibleTicketAsync(number, cancellationToken));
    }

    public async Task UpdateStatusAsync(int number, UpdateTicketStatusRequest request, CancellationToken cancellationToken = default)
    {
        var ticket = await GetAccessibleTicketAsync(number, cancellationToken, includeDetails: false);
        if (currentUser.Role == UserRoleNames.Customer && (request.Status != TicketStatus.Closed || ticket.Status != TicketStatus.Resolved))
            throw new ForbiddenException("Customers may only close their own resolved tickets.");
        if (currentUser.Role is not (UserRoleNames.Admin or UserRoleNames.SupportAgent or UserRoleNames.Customer))
            throw new ForbiddenException();

        var oldValue = ticket.Status.ToString();
        try
        {
            ticket.ChangeStatus(request.Status);
        }
        catch (InvalidOperationException exception)
        {
            throw new ValidationException(exception.Message);
        }
        if (oldValue == ticket.Status.ToString())
            return;

        dbContext.TicketActivities.Add(TicketActivity.Create(number, currentUser.UserId, "StatusChanged",
            $"Status changed from {oldValue} to {ticket.Status}.", oldValue, ticket.Status.ToString()));
        await SaveChangesAsync(cancellationToken);
    }

    public async Task UpdatePriorityAsync(int number, UpdateTicketPriorityRequest request, CancellationToken cancellationToken = default)
    {
        EnsureRole(UserRoleNames.Admin);
        var ticket = await GetAccessibleTicketAsync(number, cancellationToken, includeDetails: false);
        var oldValue = ticket.Priority.ToString();
        ticket.ChangePriority(request.Priority);
        if (oldValue == ticket.Priority.ToString())
            return;

        dbContext.TicketActivities.Add(TicketActivity.Create(number, currentUser.UserId, "PriorityChanged",
            $"Priority changed from {oldValue} to {ticket.Priority}.", oldValue, ticket.Priority.ToString()));
        await SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAssignmentAsync(int number, AssignTicketRequest request, CancellationToken cancellationToken = default)
    {
        EnsureRole(UserRoleNames.Admin);
        var ticket = await GetAccessibleTicketAsync(number, cancellationToken, includeDetails: false);
        User? agent = null;
        if (request.AgentId.HasValue)
        {
            agent = await dbContext.Users.SingleOrDefaultAsync(user => user.Id == request.AgentId &&
                user.Role == UserRole.SupportAgent && user.IsActive, cancellationToken)
                ?? throw new ValidationException("The selected support agent does not exist or is inactive.");
        }

        var oldId = ticket.AssignedSupportId;
        ticket.AssignSupport(agent?.Id);
        if (oldId == ticket.AssignedSupportId)
            return;

        var oldName = oldId.HasValue
            ? await dbContext.Users.Where(user => user.Id == oldId).Select(user => user.FirstName + " " + user.LastName).SingleOrDefaultAsync(cancellationToken)
            : null;
        var newName = agent?.FullName;
        dbContext.TicketActivities.Add(TicketActivity.Create(number, currentUser.UserId, "AssignmentChanged",
            newName is null ? "Ticket unassigned." : $"Ticket assigned to {newName}.", oldName, newName));
        await SaveChangesAsync(cancellationToken);
    }

    public async Task AddCommentAsync(int number, AddCommentRequest request, CancellationToken cancellationToken = default)
    {
        var ticket = await GetAccessibleTicketAsync(number, cancellationToken, includeDetails: false);
        if (ticket.Status == TicketStatus.Closed)
            throw new ValidationException("Comments cannot be added to a closed ticket.");
        dbContext.Comments.Add(Comment.Create(number, currentUser.UserId, request.Content));
        dbContext.TicketActivities.Add(TicketActivity.Create(number, currentUser.UserId, "CommentAdded", "A comment was added."));
        await SaveChangesAsync(cancellationToken);
    }

    public async Task LogTimeAsync(int number, LogTimeRequest request, CancellationToken cancellationToken = default)
    {
        EnsureRole(UserRoleNames.SupportAgent);
        var ticket = await GetAccessibleTicketAsync(number, cancellationToken, includeDetails: false);
        if (ticket.Status == TicketStatus.Closed)
            throw new ValidationException("Time cannot be logged against a closed ticket.");
        if (!request.WorkDate.HasValue)
            throw new ValidationException("Work date is required.");
        if (request.WorkDate.Value.Date > DateTime.UtcNow.Date)
            throw new ValidationException("Work date cannot be in the future.");
        if (request.WorkDate.Value.Date < ticket.CreatedAt.Date)
            throw new ValidationException("Work date cannot be before the ticket was created.");

        dbContext.TimeEntries.Add(TimeEntry.Create(number, currentUser.UserId, request.WorkDate.Value, request.DurationMinutes, request.Description));
        dbContext.TicketActivities.Add(TicketActivity.Create(number, currentUser.UserId, "TimeLogged",
            $"Logged {request.DurationMinutes} minutes of work.", null, request.DurationMinutes.ToString()));
        await SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<TicketActivityDto>> GetTimelineAsync(int number, CancellationToken cancellationToken = default)
    {
        _ = await GetAccessibleTicketAsync(number, cancellationToken, includeDetails: false);
        return await dbContext.TicketActivities.AsNoTracking()
            .Where(activity => activity.TicketNumber == number)
            .OrderBy(activity => activity.CreatedAt)
            .Select(activity => new TicketActivityDto(activity.Id, activity.ActivityType, activity.Description,
                activity.User == null ? null : activity.User.FirstName + " " + activity.User.LastName,
                activity.OldValue, activity.NewValue, activity.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    private IQueryable<Ticket> Scope(IQueryable<Ticket> tickets)
    {
        EnsureAuthenticated();
        return currentUser.Role switch
        {
            UserRoleNames.Admin => tickets,
            UserRoleNames.SupportAgent => tickets.Where(ticket => ticket.AssignedSupportId == currentUser.UserId),
            UserRoleNames.Customer => tickets.Where(ticket => ticket.CustomerId == currentUser.UserId),
            _ => tickets.Where(_ => false)
        };
    }

    private async Task<Ticket> GetAccessibleTicketAsync(int number, CancellationToken cancellationToken, bool includeDetails = true)
    {
        if (number <= 0)
            throw new NotFoundException("Ticket not found.");

        IQueryable<Ticket> query = dbContext.Tickets;
        if (includeDetails)
        {
            query = query.Include(ticket => ticket.Customer)
                .Include(ticket => ticket.AssignedSupport)
                .Include(ticket => ticket.Comments).ThenInclude(comment => comment.User)
                .Include(ticket => ticket.Activities).ThenInclude(activity => activity.User)
                .Include(ticket => ticket.TimeEntries).ThenInclude(entry => entry.Agent);
        }

        return await Scope(query).SingleOrDefaultAsync(ticket => ticket.Number == number, cancellationToken)
            ?? throw new NotFoundException("Ticket not found.");
    }

    private static IQueryable<Ticket> ApplySort(IQueryable<Ticket> tickets, string sortBy, bool ascending) => (sortBy, ascending) switch
    {
        ("number", true) => tickets.OrderBy(ticket => ticket.Number),
        ("number", false) => tickets.OrderByDescending(ticket => ticket.Number),
        ("title", true) => tickets.OrderBy(ticket => ticket.Title),
        ("title", false) => tickets.OrderByDescending(ticket => ticket.Title),
        ("status", true) => tickets.OrderBy(ticket => ticket.Status),
        ("status", false) => tickets.OrderByDescending(ticket => ticket.Status),
        ("priority", true) => tickets.OrderBy(ticket => ticket.Priority),
        ("priority", false) => tickets.OrderByDescending(ticket => ticket.Priority),
        ("updatedAt", true) => tickets.OrderBy(ticket => ticket.UpdatedAt),
        ("updatedAt", false) => tickets.OrderByDescending(ticket => ticket.UpdatedAt),
        (_, true) => tickets.OrderBy(ticket => ticket.CreatedAt),
        _ => tickets.OrderByDescending(ticket => ticket.CreatedAt)
    };

    private TicketDetailDto MapDetail(Ticket ticket) => new(
        ticket.Number, ticket.Title, ticket.Description, ticket.Status, ticket.Priority, ticket.CustomerId,
        ticket.Customer?.FullName ?? string.Empty, ticket.AssignedSupportId, ticket.AssignedSupport?.FullName,
        ticket.CreatedAt, ticket.UpdatedAt, ticket.TimeEntries.Sum(entry => entry.DurationMinutes), ticket.ResolvedAt, ticket.ClosedAt,
        ticket.Comments.OrderBy(comment => comment.CreatedAt)
            .Select(comment => new CommentDto(comment.Id, comment.Content, comment.UserId, comment.User?.FullName ?? string.Empty,
                comment.User?.Role ?? UserRole.Customer, comment.CreatedAt)).ToList(),
        ticket.Activities.OrderBy(activity => activity.CreatedAt)
            .Select(activity => new TicketActivityDto(activity.Id, activity.ActivityType, activity.Description,
                activity.User?.FullName, activity.OldValue, activity.NewValue, activity.CreatedAt)).ToList(),
        ticket.TimeEntries.OrderBy(entry => entry.WorkDate)
            .Select(entry => new TimeEntryDto(entry.Id, entry.AgentId, entry.Agent?.FullName ?? string.Empty, entry.WorkDate,
                entry.DurationMinutes, entry.Description, entry.CreatedAt)).ToList());

    private void EnsureAuthenticated()
    {
        if (!currentUser.IsAuthenticated)
            throw new UnauthorizedAccessException("Authentication is required.");
    }

    private void EnsureRole(string role)
    {
        EnsureAuthenticated();
        if (currentUser.Role != role)
            throw new ForbiddenException();
    }

    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The ticket was changed by another user. Refresh and retry.");
        }
    }
}
