using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SMS.Application;
using SMS.Infrastructure.Persistence;

namespace SMS.Infrastructure;

public sealed class TicketNumberGenerator(ApplicationDbContext dbContext) : ITicketNumberGenerator
{
    private const int SequenceExhaustedErrorNumber = 11728;

    public async Task<int> GetNextAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.Database.IsRelational()
            ? await GetNextRelationalValueAsync(cancellationToken)
            : await GetNextInMemoryValueAsync(cancellationToken);
    }

    private async Task<int> GetNextRelationalValueAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var number = await GetNextSequenceValueAsync(cancellationToken);
            var alreadyExists = await dbContext.Tickets.AsNoTracking()
                .AnyAsync(ticket => ticket.Number == number, cancellationToken);

            if (!alreadyExists)
                return number;
        }
    }

    private async Task<int> GetNextSequenceValueAsync(CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        try
        {
            if (shouldClose)
                await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT NEXT VALUE FOR [dbo].[TicketNumberSequence]";
            command.Transaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result, CultureInfo.InvariantCulture);
        }
        catch (SqlException exception) when (exception.Number == SequenceExhaustedErrorNumber)
        {
            throw Exhausted();
        }
        finally
        {
            if (shouldClose && connection.State == ConnectionState.Open)
                await connection.CloseAsync();
        }
    }

    private async Task<int> GetNextInMemoryValueAsync(CancellationToken cancellationToken)
    {
        var highestNumber = await dbContext.Tickets.AsNoTracking()
            .Select(ticket => (int?)ticket.Number)
            .MaxAsync(cancellationToken) ?? 0;

        return highestNumber == int.MaxValue
            ? throw Exhausted()
            : highestNumber + 1;
    }

    private static ConflictException Exhausted() =>
        new("Ticket number capacity has been reached.");
}
