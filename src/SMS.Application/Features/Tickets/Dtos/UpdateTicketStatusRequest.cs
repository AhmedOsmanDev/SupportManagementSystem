using System.ComponentModel.DataAnnotations;
using SMS.Domain;

namespace SMS.Application;

public sealed class UpdateTicketStatusRequest
{
    [EnumDataType(typeof(TicketStatus))]
    public TicketStatus Status { get; init; }
}
