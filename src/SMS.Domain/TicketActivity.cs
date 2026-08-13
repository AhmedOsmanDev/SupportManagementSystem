namespace SMS.Domain;

public sealed class TicketActivity
{
    private TicketActivity() { }

    private TicketActivity(string ticketNumber, Guid? userId, string activityType, string description, string? oldValue, string? newValue)
    {
        Id = Guid.NewGuid();
        TicketNumber = ticketNumber;
        UserId = userId;
        ActivityType = activityType;
        Description = description;
        OldValue = oldValue;
        NewValue = newValue;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string TicketNumber { get; private set; } = string.Empty;
    public Guid? UserId { get; private set; }
    public string ActivityType { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Ticket? Ticket { get; private set; }
    public User? User { get; private set; }

    public static TicketActivity Create(string ticketNumber, Guid? userId, string activityType, string description, string? oldValue = null, string? newValue = null) =>
        new(ticketNumber, userId, activityType, description, oldValue, newValue);
}
