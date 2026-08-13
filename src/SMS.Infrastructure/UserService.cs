using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SMS.Application;
using SMS.Domain;
using SMS.Infrastructure.Persistence;

namespace SMS.Infrastructure;

public sealed class UserService(
    ApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IPasswordHasher<User> passwordHasher) : IUserService
{
    public async Task<IReadOnlyCollection<ManagedUserDto>> GetUsersAsync(
        UserRole? role,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin();
        var users = dbContext.Users.AsNoTracking();
        if (role.HasValue)
            users = users.Where(user => user.Role == role.Value);
        if (activeOnly)
            users = users.Where(user => user.IsActive);

        return await users.OrderBy(user => user.FirstName).ThenBy(user => user.LastName)
            .Select(user => new ManagedUserDto(user.Id, user.FirstName, user.LastName, user.Email, user.Role,
                user.IsActive, user.CreatedAt,
                dbContext.Tickets.Count(ticket => ticket.AssignedSupportId == user.Id &&
                    ticket.Status != TicketStatus.Resolved && ticket.Status != TicketStatus.Closed)))
            .ToListAsync(cancellationToken);
    }

    public async Task<ManagedUserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAdmin();
        if (!Enum.IsDefined(request.Role))
            throw new ValidationException("A valid role is required.");

        var email = request.Email.Trim().ToLowerInvariant();
        if (await dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken))
            throw new ConflictException("A user with this email already exists.");

        var user = User.Create(request.FirstName, request.LastName, email, string.Empty, request.Role);
        user.SetPasswordHash(passwordHasher.HashPassword(user, request.Password));
        dbContext.Users.Add(user);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException("A user with this email already exists.");
        }
        return new ManagedUserDto(user.Id, user.FirstName, user.LastName, user.Email, user.Role,
            user.IsActive, user.CreatedAt, 0);
    }

    public async Task SetActiveAsync(Guid id, UpdateUserStatusRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAdmin();
        if (id == currentUser.UserId && !request.IsActive)
            throw new ValidationException("You cannot deactivate your own account.");

        var user = await dbContext.Users.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            ?? throw new NotFoundException("User not found.");
        user.SetActive(request.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private void EnsureAdmin()
    {
        if (!currentUser.IsAuthenticated)
            throw new UnauthorizedAccessException("Authentication is required.");
        if (currentUser.Role != UserRole.Admin)
            throw new ForbiddenException();
    }
}
