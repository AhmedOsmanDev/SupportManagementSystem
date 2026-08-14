using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SMS.Application;
using SMS.Domain;
using SMS.Infrastructure;
using SMS.Infrastructure.Persistence;

namespace SMS.IntegrationTests;

public sealed class TicketNumberGeneratorTests
{
    private static readonly Guid CustomerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Theory]
    [InlineData(null, 1)]
    [InlineData(1, 2)]
    [InlineData(9, 10)]
    [InlineData(16, 17)]
    public async Task GetNextAsync_ReturnsNextInteger(int? existingNumber, int expected)
    {
        await using var context = CreateContext();
        if (existingNumber is not null)
        {
            context.Tickets.Add(Ticket.Create(
                existingNumber.Value,
                "Existing ticket",
                "Existing ticket used to verify number allocation.",
                TicketPriority.Medium,
                CustomerId));
            await context.SaveChangesAsync();
        }

        var generator = new TicketNumberGenerator(context);

        var number = await generator.GetNextAsync();

        number.Should().Be(expected);
    }

    [Fact]
    public async Task GetNextAsync_WhenIntegerCapacityIsExhausted_ThrowsConflict()
    {
        await using var context = CreateContext();
        context.Tickets.Add(Ticket.Create(
            int.MaxValue,
            "Final ticket",
            "The final integer ticket number is already allocated.",
            TicketPriority.Medium,
            CustomerId));
        await context.SaveChangesAsync();
        var generator = new TicketNumberGenerator(context);

        var act = () => generator.GetNextAsync();

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*capacity*");
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"ticket-number-generator-{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }
}
