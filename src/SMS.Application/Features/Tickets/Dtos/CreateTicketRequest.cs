using System.ComponentModel.DataAnnotations;
using SMS.Domain;

namespace SMS.Application;

public sealed class CreateTicketRequest
{
    [Required, StringLength(200, MinimumLength = 3)]
    public string Title { get; init; } = string.Empty;

    [Required, StringLength(5000, MinimumLength = 10)]
    public string Description { get; init; } = string.Empty;

    [EnumDataType(typeof(TicketPriority))]
    public TicketPriority Priority { get; init; } = TicketPriority.Medium;
}
