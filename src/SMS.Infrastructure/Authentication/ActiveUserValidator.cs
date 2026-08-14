using Microsoft.EntityFrameworkCore;
using SMS.Application;
using SMS.Domain;
using SMS.Infrastructure.Persistence;

namespace SMS.Infrastructure;

public sealed class ActiveUserValidator(ApplicationDbContext dbContext) : IActiveUserValidator
{
    public async Task<bool> IsValidAsync(
        Guid userId,
        string role,
        CancellationToken cancellationToken = default)
    {
        var persistedRole = await dbContext.Users.AsNoTracking()
            .Where(user => user.Id == userId && user.IsActive)
            .Select(user => (UserRole?)user.Role)
            .SingleOrDefaultAsync(cancellationToken);

        return persistedRole.HasValue &&
            string.Equals(persistedRole.Value.ToString(), role, StringComparison.Ordinal);
    }
}
