namespace SMS.Domain;

public sealed class TicketActivity
{
    private TicketActivity() { }

    private TicketActivity(int ticketNumber, Guid? userId, string activityType, string description, string? oldValue, string? newValue)
    {
        if (ticketNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(ticketNumber), "Ticket number must be greater than zero.");

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

    public int TicketNumber { get; private set; }
    public Guid? UserId { get; private set; }

    public string ActivityType { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public Ticket? Ticket { get; private set; }
    public User? User { get; private set; }

    public static TicketActivity Create(int ticketNumber, Guid? userId, string activityType, string description, string? oldValue = null, string? newValue = null) =>
        new(ticketNumber, userId, activityType, description, oldValue, newValue);
}
