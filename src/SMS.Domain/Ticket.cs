namespace SMS.Domain;

public sealed class Ticket
{
    private readonly List<Comment> _comments = [];
    private readonly List<TicketActivity> _activities = [];
    private readonly List<TimeEntry> _timeEntries = [];

    private Ticket() { }

    private Ticket(string number, string title, string description, TicketPriority priority, Guid customerId)
    {
        Number = number;
        Title = title.Trim();
        Description = description.Trim();
        Priority = priority;
        CustomerId = customerId;
        Status = TicketStatus.Open;
        CreatedAt = UpdatedAt = DateTime.UtcNow;
    }

    public string Number { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public TicketStatus Status { get; private set; }
    public TicketPriority Priority { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid? AssignedSupportId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public User? Customer { get; private set; }
    public User? AssignedSupport { get; private set; }
    public IReadOnlyCollection<Comment> Comments => _comments;
    public IReadOnlyCollection<TicketActivity> Activities => _activities;
    public IReadOnlyCollection<TimeEntry> TimeEntries => _timeEntries;
    public int TotalTimeMinutes => _timeEntries.Sum(entry => entry.DurationMinutes);

    public static Ticket Create(string number, string title, string description, TicketPriority priority, Guid customerId) =>
        new(number, title, description, priority, customerId);

    public void ChangeStatus(TicketStatus nextStatus)
    {
        if (nextStatus == Status)
            return;

        if (!CanTransition(Status, nextStatus))
            throw new InvalidOperationException($"A ticket cannot transition from {Status} to {nextStatus}.");

        Status = nextStatus;
        UpdatedAt = DateTime.UtcNow;
        if (nextStatus == TicketStatus.Resolved)
            ResolvedAt = UpdatedAt;
        else if (nextStatus == TicketStatus.InProgress)
            ResolvedAt = null;
        else if (nextStatus == TicketStatus.Closed)
            ClosedAt = UpdatedAt;
    }

    public void ChangePriority(TicketPriority priority)
    {
        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignSupport(Guid? supportId)
    {
        AssignedSupportId = supportId;
        UpdatedAt = DateTime.UtcNow;
    }

    public static bool CanTransition(TicketStatus current, TicketStatus next) => current switch
    {
        TicketStatus.Open => next == TicketStatus.InProgress,
        TicketStatus.InProgress => next == TicketStatus.Resolved,
        TicketStatus.Resolved => next is TicketStatus.InProgress or TicketStatus.Closed,
        TicketStatus.Closed => false,
        _ => false
    };
}
