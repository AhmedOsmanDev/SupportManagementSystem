namespace SMS.Domain;

public sealed class Comment
{
    private Comment() { }

    private Comment(int ticketNumber, Guid userId, string content)
    {
        if (ticketNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(ticketNumber), "Ticket number must be greater than zero.");

        Id = Guid.NewGuid();
        TicketNumber = ticketNumber;
        UserId = userId;
        Content = content.Trim();
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public int TicketNumber { get; private set; }
    public Guid UserId { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    public Ticket? Ticket { get; private set; }
    public User? User { get; private set; }

    public static Comment Create(int ticketNumber, Guid userId, string content) => new(ticketNumber, userId, content);
}
