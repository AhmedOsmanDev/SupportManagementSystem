namespace SMS.Application;

public interface ITicketNumberGenerator
{
    Task<int> GetNextAsync(CancellationToken cancellationToken = default);
}
