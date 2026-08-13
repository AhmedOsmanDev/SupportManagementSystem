using FluentAssertions;
using SMS.Domain;

namespace SMS.UnitTests;

public sealed class TicketTests
{
    private static readonly Guid CustomerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Create_InitializesOpenTicketAndNormalizesText()
    {
        var before = DateTime.UtcNow;

        var ticket = Ticket.Create(
            "TKT-000001",
            "  Printer unavailable  ",
            "  The reception printer cannot be reached.  ",
            TicketPriority.High,
            CustomerId);

        ticket.Number.Should().Be("TKT-000001");
        ticket.Title.Should().Be("Printer unavailable");
        ticket.Description.Should().Be("The reception printer cannot be reached.");
        ticket.CustomerId.Should().Be(CustomerId);
        ticket.Status.Should().Be(TicketStatus.Open);
        ticket.Priority.Should().Be(TicketPriority.High);
        ticket.AssignedSupportId.Should().BeNull();
        ticket.CreatedAt.Should().BeOnOrAfter(before);
        ticket.UpdatedAt.Should().Be(ticket.CreatedAt);
        ticket.ResolvedAt.Should().BeNull();
        ticket.ClosedAt.Should().BeNull();
        ticket.TotalTimeMinutes.Should().Be(0);
    }

    [Theory]
    [InlineData(TicketStatus.Open, TicketStatus.InProgress)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Resolved)]
    [InlineData(TicketStatus.Resolved, TicketStatus.InProgress)]
    [InlineData(TicketStatus.Resolved, TicketStatus.Closed)]
    public void CanTransition_ForSupportedTransition_ReturnsTrue(
        TicketStatus current,
        TicketStatus next)
    {
        Ticket.CanTransition(current, next).Should().BeTrue();
    }

    [Theory]
    [InlineData(TicketStatus.Open, TicketStatus.Resolved)]
    [InlineData(TicketStatus.Open, TicketStatus.Closed)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Open)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Closed)]
    [InlineData(TicketStatus.Resolved, TicketStatus.Open)]
    [InlineData(TicketStatus.Closed, TicketStatus.Open)]
    [InlineData(TicketStatus.Closed, TicketStatus.InProgress)]
    [InlineData(TicketStatus.Closed, TicketStatus.Resolved)]
    public void ChangeStatus_ForUnsupportedTransition_Throws(
        TicketStatus current,
        TicketStatus next)
    {
        var ticket = TicketAt(current);

        var act = () => ticket.ChangeStatus(next);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{current}*{next}*");
    }

    [Fact]
    public void ChangeStatus_WhenResolved_RecordsResolutionTimestamp()
    {
        var ticket = TicketAt(TicketStatus.InProgress);

        ticket.ChangeStatus(TicketStatus.Resolved);

        ticket.Status.Should().Be(TicketStatus.Resolved);
        ticket.ResolvedAt.Should().NotBeNull();
        ticket.ResolvedAt.Should().Be(ticket.UpdatedAt);
    }

    [Fact]
    public void ChangeStatus_WhenResolvedTicketIsReopened_ClearsResolutionTimestamp()
    {
        var ticket = TicketAt(TicketStatus.Resolved);
        ticket.ResolvedAt.Should().NotBeNull();

        ticket.ChangeStatus(TicketStatus.InProgress);

        ticket.Status.Should().Be(TicketStatus.InProgress);
        ticket.ResolvedAt.Should().BeNull();
    }

    [Fact]
    public void ChangeStatus_WhenClosed_RecordsClosedTimestampAndBecomesTerminal()
    {
        var ticket = TicketAt(TicketStatus.Resolved);

        ticket.ChangeStatus(TicketStatus.Closed);

        ticket.ClosedAt.Should().NotBeNull();
        ticket.ClosedAt.Should().Be(ticket.UpdatedAt);
        var act = () => ticket.ChangeStatus(TicketStatus.InProgress);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AssignSupport_AllowsAssignmentAndUnassignment()
    {
        var ticket = TicketAt(TicketStatus.Open);
        var agentId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        ticket.AssignSupport(agentId);
        ticket.AssignedSupportId.Should().Be(agentId);

        ticket.AssignSupport(null);
        ticket.AssignedSupportId.Should().BeNull();
    }

    [Fact]
    public void ChangePriority_UpdatesPriority()
    {
        var ticket = TicketAt(TicketStatus.Open);

        ticket.ChangePriority(TicketPriority.Critical);

        ticket.Priority.Should().Be(TicketPriority.Critical);
        ticket.UpdatedAt.Should().BeOnOrAfter(ticket.CreatedAt);
    }

    private static Ticket TicketAt(TicketStatus status)
    {
        var ticket = Ticket.Create(
            $"TKT-{Guid.NewGuid():N}",
            "Ticket title",
            "Ticket description long enough",
            TicketPriority.Medium,
            CustomerId);

        if (status >= TicketStatus.InProgress)
            ticket.ChangeStatus(TicketStatus.InProgress);
        if (status >= TicketStatus.Resolved)
            ticket.ChangeStatus(TicketStatus.Resolved);
        if (status >= TicketStatus.Closed)
            ticket.ChangeStatus(TicketStatus.Closed);

        return ticket;
    }
}

