using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application;
using SMS.Domain;

namespace SMS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/tickets")]
public sealed class TicketsController(ITicketService ticketService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<TicketSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TicketSummaryDto>>> GetTickets(
        [FromQuery] TicketQuery query,
        CancellationToken cancellationToken) =>
        Ok(await ticketService.GetTicketsAsync(query, cancellationToken));

    [HttpGet("{number}")]
    [ProducesResponseType<TicketDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketDetailDto>> GetTicket(string number, CancellationToken cancellationToken) =>
        Ok(await ticketService.GetTicketAsync(number, cancellationToken));

    [Authorize(Roles = nameof(UserRole.Customer))]
    [HttpPost]
    [ProducesResponseType<TicketDetailDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<TicketDetailDto>> CreateTicket(CreateTicketRequest request, CancellationToken cancellationToken)
    {
        var ticket = await ticketService.CreateTicketAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetTicket), new { number = ticket.Number }, ticket);
    }

    [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.SupportAgent)},{nameof(UserRole.Customer)}")]
    [HttpPatch("{number}/status")]
    public async Task<IActionResult> UpdateStatus(string number, UpdateTicketStatusRequest request, CancellationToken cancellationToken)
    {
        await ticketService.UpdateStatusAsync(number, request, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPatch("{number}/priority")]
    public async Task<IActionResult> UpdatePriority(string number, UpdateTicketPriorityRequest request, CancellationToken cancellationToken)
    {
        await ticketService.UpdatePriorityAsync(number, request, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPatch("{number}/assignment")]
    public async Task<IActionResult> UpdateAssignment(string number, AssignTicketRequest request, CancellationToken cancellationToken)
    {
        await ticketService.UpdateAssignmentAsync(number, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("{number}/comments")]
    public async Task<IActionResult> AddComment(string number, AddCommentRequest request, CancellationToken cancellationToken)
    {
        await ticketService.AddCommentAsync(number, request, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = nameof(UserRole.SupportAgent))]
    [HttpPost("{number}/time-entries")]
    public async Task<IActionResult> LogTime(string number, LogTimeRequest request, CancellationToken cancellationToken)
    {
        await ticketService.LogTimeAsync(number, request, cancellationToken);
        return NoContent();
    }

    [HttpGet("{number}/timeline")]
    [ProducesResponseType<IReadOnlyCollection<TicketActivityDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<TicketActivityDto>>> GetTimeline(string number, CancellationToken cancellationToken) =>
        Ok(await ticketService.GetTimelineAsync(number, cancellationToken));
}
