namespace SMS.Domain;

public sealed class Comment
{
    private Comment() { }

    private Comment(string ticketNumber, Guid userId, string content)
    {
        Id = Guid.NewGuid();
        TicketNumber = ticketNumber;
        UserId = userId;
        Content = content.Trim();
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string TicketNumber { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public Ticket? Ticket { get; private set; }
    public User? User { get; private set; }

    public static Comment Create(string ticketNumber, Guid userId, string content) => new(ticketNumber, userId, content);
}
