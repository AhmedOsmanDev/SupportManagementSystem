namespace SMS.Application;

public interface ICurrentUser
{
    Guid UserId { get; }
    string Email { get; }
    string Role { get; }
    bool IsAuthenticated { get; }
}
