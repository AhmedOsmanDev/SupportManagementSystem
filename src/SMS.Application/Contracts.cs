using SMS.Domain;

namespace SMS.Application;

public interface ICurrentUser
{
    Guid UserId { get; }
    string Email { get; }
    UserRole Role { get; }
    bool IsAuthenticated { get; }
}

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<UserDto> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}

public interface ITokenGenerator
{
    TokenResult Create(User user);
}

public sealed record TokenResult(string AccessToken, DateTime ExpiresAt);

public interface ITicketService
{
    Task<PagedResult<TicketSummaryDto>> GetTicketsAsync(TicketQuery query, CancellationToken cancellationToken = default);
    Task<TicketDetailDto> GetTicketAsync(string number, CancellationToken cancellationToken = default);
    Task<TicketDetailDto> CreateTicketAsync(CreateTicketRequest request, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(string number, UpdateTicketStatusRequest request, CancellationToken cancellationToken = default);
    Task UpdatePriorityAsync(string number, UpdateTicketPriorityRequest request, CancellationToken cancellationToken = default);
    Task UpdateAssignmentAsync(string number, AssignTicketRequest request, CancellationToken cancellationToken = default);
    Task AddCommentAsync(string number, AddCommentRequest request, CancellationToken cancellationToken = default);
    Task LogTimeAsync(string number, LogTimeRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TicketActivityDto>> GetTimelineAsync(string number, CancellationToken cancellationToken = default);
}

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}

public interface IUserService
{
    Task<IReadOnlyCollection<ManagedUserDto>> GetUsersAsync(UserRole? role, bool activeOnly, CancellationToken cancellationToken = default);
    Task<ManagedUserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task SetActiveAsync(Guid id, UpdateUserStatusRequest request, CancellationToken cancellationToken = default);
}
