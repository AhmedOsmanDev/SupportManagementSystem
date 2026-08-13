using System.ComponentModel.DataAnnotations;
using SMS.Domain;

namespace SMS.Application;

public sealed class LoginRequest
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;
}

public sealed record AuthResponse(string AccessToken, DateTime ExpiresAt, UserDto User);

public record UserDto(Guid Id, string FirstName, string LastName, string Email, UserRole Role);

public sealed record ManagedUserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt,
    int AssignedTicketCount);

public sealed class CreateUserRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string FirstName { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    public string LastName { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;

    [EnumDataType(typeof(UserRole))]
    public UserRole Role { get; init; }
}

public sealed class UpdateUserStatusRequest
{
    public bool IsActive { get; init; }
}

public sealed class TicketQuery
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;

    [StringLength(200)]
    public string? Search { get; init; }

    public TicketStatus? Status { get; init; }
    public TicketPriority? Priority { get; init; }

    [RegularExpression("^(createdAt|updatedAt|priority|status|title|number)$", ErrorMessage = "Unsupported sort field.")]
    public string SortBy { get; init; } = "createdAt";

    [RegularExpression("^(asc|desc)$", ErrorMessage = "Sort direction must be asc or desc.")]
    public string SortDirection { get; init; } = "desc";
}

public sealed class CreateTicketRequest
{
    [Required, StringLength(200, MinimumLength = 3)]
    public string Title { get; init; } = string.Empty;

    [Required, StringLength(5000, MinimumLength = 10)]
    public string Description { get; init; } = string.Empty;

    [EnumDataType(typeof(TicketPriority))]
    public TicketPriority Priority { get; init; } = TicketPriority.Medium;
}

public sealed class UpdateTicketStatusRequest
{
    [EnumDataType(typeof(TicketStatus))]
    public TicketStatus Status { get; init; }
}

public sealed class UpdateTicketPriorityRequest
{
    [EnumDataType(typeof(TicketPriority))]
    public TicketPriority Priority { get; init; }
}

public sealed class AssignTicketRequest
{
    public Guid? AgentId { get; init; }
}

public sealed class AddCommentRequest
{
    [Required, StringLength(4000, MinimumLength = 1)]
    public string Content { get; init; } = string.Empty;
}

public sealed class LogTimeRequest
{
    [Required]
    public DateTime? WorkDate { get; init; }

    [Range(1, 1440)]
    public int DurationMinutes { get; init; }

    [Required, StringLength(1000, MinimumLength = 2)]
    public string Description { get; init; } = string.Empty;
}

public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);

public record TicketSummaryDto(
    string Number,
    string Title,
    string Description,
    TicketStatus Status,
    TicketPriority Priority,
    Guid CustomerId,
    string CustomerName,
    Guid? AssignedAgentId,
    string? AssignedAgentName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int TotalTimeMinutes);

public sealed record TicketDetailDto(
    string Number,
    string Title,
    string Description,
    TicketStatus Status,
    TicketPriority Priority,
    Guid CustomerId,
    string CustomerName,
    Guid? AssignedAgentId,
    string? AssignedAgentName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int TotalTimeMinutes,
    DateTime? ResolvedAt,
    DateTime? ClosedAt,
    IReadOnlyCollection<CommentDto> Comments,
    IReadOnlyCollection<TicketActivityDto> Activities,
    IReadOnlyCollection<TimeEntryDto> TimeEntries);

public sealed record CommentDto(Guid Id, string Content, Guid AuthorId, string AuthorName, UserRole AuthorRole, DateTime CreatedAt);

public sealed record TicketActivityDto(
    Guid Id,
    string Type,
    string Description,
    string? PerformedBy,
    string? OldValue,
    string? NewValue,
    DateTime CreatedAt);

public sealed record TimeEntryDto(
    Guid Id,
    Guid AgentId,
    string AgentName,
    DateTime WorkDate,
    int DurationMinutes,
    string Description,
    DateTime CreatedAt);

public sealed record AgentWorkloadDto(Guid AgentId, string AgentName, int ActiveTickets, int TotalMinutes);

public sealed record DashboardDto(
    int TotalTickets,
    int OpenTickets,
    int InProgressTickets,
    int ResolvedTickets,
    int ClosedTickets,
    int OpenCriticalTickets,
    double AverageResolutionHours,
    IReadOnlyCollection<AgentWorkloadDto> AgentWorkload);
