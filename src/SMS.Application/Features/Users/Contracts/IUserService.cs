namespace SMS.Application;

public interface IUserService
{
    Task<IReadOnlyCollection<ManagedUserDto>> GetUsersAsync(UserRoleFilter? role, bool activeOnly, CancellationToken cancellationToken = default);
    Task<ManagedUserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task SetActiveAsync(Guid id, UpdateUserStatusRequest request, CancellationToken cancellationToken = default);
}
