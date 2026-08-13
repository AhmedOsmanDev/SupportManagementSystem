namespace SMS.Domain;

public sealed class TimeEntry
{
    private TimeEntry() { }

    private TimeEntry(string ticketNumber, Guid agentId, DateTime workDate, int durationMinutes, string description)
    {
        Id = Guid.NewGuid();
        TicketNumber = ticketNumber;
        AgentId = agentId;
        WorkDate = DateTime.SpecifyKind(workDate.Date, DateTimeKind.Utc);
        DurationMinutes = durationMinutes;
        Description = description.Trim();
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string TicketNumber { get; private set; } = string.Empty;
    public Guid AgentId { get; private set; }
    public DateTime WorkDate { get; private set; }
    public int DurationMinutes { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public Ticket? Ticket { get; private set; }
    public User? Agent { get; private set; }

    public static TimeEntry Create(string ticketNumber, Guid agentId, DateTime workDate, int durationMinutes, string description)
    {
        if (durationMinutes is < 1 or > 1440)
            throw new ArgumentOutOfRangeException(nameof(durationMinutes), "Duration must be between 1 and 1440 minutes.");
        return new(ticketNumber, agentId, workDate, durationMinutes, description);
    }
}
