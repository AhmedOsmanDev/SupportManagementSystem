using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Globalization;
using FluentAssertions;
using SMS.Testing;

namespace SMS.DataIsolationTests;

public sealed class AgentAssignmentIsolationTests(ApiTestFactory factory) : IClassFixture<ApiTestFactory>
{
    [Fact]
    public async Task UnassignedAgent_CannotListReadOrMutateAnotherAgentsTicket()
    {
        using var customer = await factory.CreateAuthenticatedClientAsync(
            "customer@support.local",
            "Customer123!");
        using var admin = await factory.CreateAuthenticatedClientAsync(
            "admin@support.local",
            "Admin123!");
        var ticketNumber = await CreateTicketAsync(customer);
        var seededAgentId = await GetSeededAgentIdAsync(admin);
        using var assignment = await admin.PatchAsJsonAsync(
            $"/api/tickets/{ticketNumber}/assignment",
            new { agentId = seededAgentId });
        assignment.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var unique = Guid.NewGuid().ToString("N");
        var email = $"other.agent.{unique}@support.local";
        const string password = "OtherAgent123!";
        using var createAgent = await admin.PostAsJsonAsync("/api/users", new
        {
            firstName = "Other",
            lastName = "Agent",
            email,
            password,
            role = "SupportAgent"
        });
        createAgent.StatusCode.Should().Be(HttpStatusCode.Created);
        using var otherAgent = await factory.CreateAuthenticatedClientAsync(email, password);

        using var list = await otherAgent.GetAsync("/api/tickets?page=1&pageSize=100");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        (await list.Content.ReadAsStringAsync()).Should().NotContain(
            $"\"number\":{ticketNumber.ToString(CultureInfo.InvariantCulture)}");

        using var detail = await otherAgent.GetAsync($"/api/tickets/{ticketNumber}");
        detail.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var status = await otherAgent.PatchAsJsonAsync(
            $"/api/tickets/{ticketNumber}/status",
            new { status = "InProgress" });
        status.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var time = await otherAgent.PostAsJsonAsync(
            $"/api/tickets/{ticketNumber}/time-entries",
            new
            {
                workDate = DateTime.UtcNow.Date,
                durationMinutes = 10,
                description = "Must not be persisted"
            });
        time.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<int> CreateTicketAsync(HttpClient customer)
    {
        using var response = await customer.PostAsJsonAsync("/api/tickets", new
        {
            title = $"Agent isolation {Guid.NewGuid():N}",
            description = "Only the specifically assigned support agent may access this ticket.",
            priority = "High"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        ApiTestClientExtensions.TryGetPropertyIgnoringCase(payload.RootElement, "number", out var number).Should().BeTrue();
        number.ValueKind.Should().Be(JsonValueKind.Number);
        return number.GetInt32();
    }

    private static async Task<Guid> GetSeededAgentIdAsync(HttpClient admin)
    {
        using var response = await admin.GetAsync("/api/users?role=SupportAgent&activeOnly=true");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var agent = payload.RootElement.EnumerateArray().Single(user =>
            user.TryGetProperty("email", out var email) && email.GetString() == "agent@support.local");
        return Guid.Parse(agent.GetProperty("id").GetString()!);
    }
}
