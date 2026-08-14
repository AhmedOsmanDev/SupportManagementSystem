namespace SMS.Application;

public interface IActiveUserValidator
{
    Task<bool> IsValidAsync(Guid userId, string role, CancellationToken cancellationToken = default);
}
