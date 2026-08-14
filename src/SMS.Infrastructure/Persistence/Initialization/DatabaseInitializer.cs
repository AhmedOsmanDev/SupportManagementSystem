using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SMS.Domain;

namespace SMS.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static readonly Guid AdminId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid AgentId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    public static readonly Guid CustomerId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    public static readonly Guid OtherCustomerId = Guid.Parse("30000000-0000-0000-0000-000000000002");

    public static async Task InitializeAsync(
        IServiceProvider services,
        bool seedDemoData,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        if (dbContext.Database.IsRelational())
            await dbContext.Database.MigrateAsync(cancellationToken);
        else
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        if (seedDemoData)
            await SeedAsync(scope.ServiceProvider, cancellationToken);
        logger.LogInformation("Database initialization completed (demo seed enabled: {SeedDemoData})", seedDemoData);
    }

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        var hasher = services.GetRequiredService<IPasswordHasher<User>>();
        var isRelational = dbContext.Database.IsRelational();

        await using var transaction = isRelational
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var seedUsers = new[]
        {
            CreateUser(AdminId, "Amina", "Admin", "admin@support.local", "Admin123!", UserRole.Admin, hasher),
            CreateUser(AgentId, "Sam", "Agent", "agent@support.local", "Agent123!", UserRole.SupportAgent, hasher),
            CreateUser(CustomerId, "Casey", "Customer", "customer@support.local", "Customer123!", UserRole.Customer, hasher),
            CreateUser(OtherCustomerId, "Jordan", "Customer", "customer2@support.local", "Customer123!", UserRole.Customer, hasher)
        };

        var existingEmails = await dbContext.Users.Select(user => user.Email).ToListAsync(cancellationToken);
        foreach (var user in seedUsers.Where(user => !existingEmails.Contains(user.Email)))
            dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (!await dbContext.Tickets.AnyAsync(cancellationToken))
        {
            var critical = Ticket.Create(1, "Checkout unavailable",
                "Customers receive an error while completing checkout in the production storefront.", TicketPriority.Critical, CustomerId);
            critical.AssignSupport(AgentId);
            critical.ChangeStatus(TicketStatus.InProgress);

            var open = Ticket.Create(2, "Update billing address",
                "Please update the billing address used on future invoices for our organization.", TicketPriority.Medium, CustomerId);
            var resolved = Ticket.Create(3, "Password reset email delayed",
                "Password reset emails were delayed for several minutes and should be investigated.", TicketPriority.High, OtherCustomerId);
            resolved.AssignSupport(AgentId);
            resolved.ChangeStatus(TicketStatus.InProgress);
            resolved.ChangeStatus(TicketStatus.Resolved);

            dbContext.Tickets.AddRange(critical, open, resolved);
            dbContext.TicketActivities.AddRange(
                TicketActivity.Create(critical.Number, AdminId, "Created", "Seeded demonstration ticket."),
                TicketActivity.Create(critical.Number, AdminId, "AssignmentChanged", "Ticket assigned to Sam Agent.", null, "Sam Agent"),
                TicketActivity.Create(critical.Number, AgentId, "StatusChanged", "Status changed from Open to InProgress.", "Open", "InProgress"),
                TicketActivity.Create(open.Number, CustomerId, "Created", "Seeded demonstration ticket."),
                TicketActivity.Create(resolved.Number, OtherCustomerId, "Created", "Seeded demonstration ticket."),
                TicketActivity.Create(resolved.Number, AgentId, "StatusChanged", "Ticket resolved.", "InProgress", "Resolved"));
            dbContext.Comments.Add(Comment.Create(critical.Number, CustomerId, "This is blocking all purchases for our team."));
            dbContext.TimeEntries.Add(TimeEntry.Create(critical.Number, AgentId, DateTime.UtcNow.Date, 45, "Reproduced the issue and reviewed application logs."));
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }

    private static User CreateUser(
        Guid id,
        string firstName,
        string lastName,
        string email,
        string password,
        UserRole role,
        IPasswordHasher<User> hasher)
    {
        var user = User.Create(id, firstName, lastName, email, string.Empty, role);
        user.SetPasswordHash(hasher.HashPassword(user, password));
        return user;
    }
}
