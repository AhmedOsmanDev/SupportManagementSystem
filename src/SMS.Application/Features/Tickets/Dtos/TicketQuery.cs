using System.ComponentModel.DataAnnotations;
using SMS.Domain;

namespace SMS.Application;

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
