using System.ComponentModel.DataAnnotations;
using SMS.Domain;

namespace SMS.Application;

public sealed class UpdateTicketPriorityRequest
{
    [EnumDataType(typeof(TicketPriority))]
    public TicketPriority Priority { get; init; }
}
