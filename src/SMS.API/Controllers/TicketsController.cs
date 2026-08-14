using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application;

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

    [HttpGet("{number:int}")]
    [ProducesResponseType<TicketDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketDetailDto>> GetTicket(int number, CancellationToken cancellationToken) =>
        Ok(await ticketService.GetTicketAsync(number, cancellationToken));

    [Authorize(Roles = UserRoleNames.Customer)]
    [HttpPost]
    [ProducesResponseType<TicketDetailDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<TicketDetailDto>> CreateTicket(CreateTicketRequest request, CancellationToken cancellationToken)
    {
        var ticket = await ticketService.CreateTicketAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetTicket), new { number = ticket.Number }, ticket);
    }

    [Authorize(Roles = UserRoleNames.All)]
    [HttpPatch("{number:int}/status")]
    public async Task<IActionResult> UpdateStatus(int number, UpdateTicketStatusRequest request, CancellationToken cancellationToken)
    {
        await ticketService.UpdateStatusAsync(number, request, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = UserRoleNames.Admin)]
    [HttpPatch("{number:int}/priority")]
    public async Task<IActionResult> UpdatePriority(int number, UpdateTicketPriorityRequest request, CancellationToken cancellationToken)
    {
        await ticketService.UpdatePriorityAsync(number, request, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = UserRoleNames.Admin)]
    [HttpPatch("{number:int}/assignment")]
    public async Task<IActionResult> UpdateAssignment(int number, AssignTicketRequest request, CancellationToken cancellationToken)
    {
        await ticketService.UpdateAssignmentAsync(number, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("{number:int}/comments")]
    public async Task<IActionResult> AddComment(int number, AddCommentRequest request, CancellationToken cancellationToken)
    {
        await ticketService.AddCommentAsync(number, request, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = UserRoleNames.SupportAgent)]
    [HttpPost("{number:int}/time-entries")]
    public async Task<IActionResult> LogTime(int number, LogTimeRequest request, CancellationToken cancellationToken)
    {
        await ticketService.LogTimeAsync(number, request, cancellationToken);
        return NoContent();
    }

    [HttpGet("{number:int}/timeline")]
    [ProducesResponseType<IReadOnlyCollection<TicketActivityDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<TicketActivityDto>>> GetTimeline(int number, CancellationToken cancellationToken) =>
        Ok(await ticketService.GetTimelineAsync(number, cancellationToken));
}
