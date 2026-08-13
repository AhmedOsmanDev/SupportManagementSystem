using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SMS.Testing;

namespace SMS.IntegrationTests;

public sealed class TicketWorkflowTests(ApiTestFactory factory) : IClassFixture<ApiTestFactory>
{
    [Fact]
    public async Task AssignedTicket_CanProgressThroughCommentTimeResolveAndCustomerClose()
    {
        using var customer = await factory.CreateAuthenticatedClientAsync(
            "customer@support.local",
            "Customer123!");
        using var admin = await factory.CreateAuthenticatedClientAsync(
            "admin@support.local",
            "Admin123!");
        using var agent = await factory.CreateAuthenticatedClientAsync(
            "agent@support.local",
            "Agent123!");

        var agentId = await GetSeededAgentIdAsync(admin);
        var ticketNumber = await CreateTicketAsync(customer);

        await ShouldSucceedAsync(admin.PatchAsJsonAsync(
            $"/api/tickets/{ticketNumber}/assignment",
            new { agentId }));
        await ShouldSucceedAsync(admin.PatchAsJsonAsync(
            $"/api/tickets/{ticketNumber}/priority",
            new { priority = "Critical" }));
        await ShouldSucceedAsync(agent.PatchAsJsonAsync(
            $"/api/tickets/{ticketNumber}/status",
            new { status = "InProgress" }));
        await ShouldSucceedAsync(agent.PostAsJsonAsync(
            $"/api/tickets/{ticketNumber}/comments",
            new { content = "Driver was updated and connectivity was restored." }));
        await ShouldSucceedAsync(agent.PostAsJsonAsync(
            $"/api/tickets/{ticketNumber}/time-entries",
            new
            {
                workDate = DateTime.UtcNow.Date,
                durationMinutes = 35,
                description = "Diagnosis and driver update"
            }));
        await ShouldSucceedAsync(agent.PostAsJsonAsync(
            $"/api/tickets/{ticketNumber}/time-entries",
            new
            {
                workDate = DateTime.UtcNow.Date,
                durationMinutes = 20,
                description = "Connectivity verification"
            }));
        await ShouldSucceedAsync(agent.PatchAsJsonAsync(
            $"/api/tickets/{ticketNumber}/status",
            new { status = "Resolved" }));
        await ShouldSucceedAsync(customer.PatchAsJsonAsync(
            $"/api/tickets/{ticketNumber}/status",
            new { status = "Closed" }));

        using var detailResponse = await customer.GetAsync($"/api/tickets/{ticketNumber}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var detail = JsonDocument.Parse(await detailResponse.Content.ReadAsStringAsync());
        ReadString(detail.RootElement, "status").Should().Be("Closed");
        ReadString(detail.RootElement, "priority").Should().Be("Critical");
        ReadInt(detail.RootElement, "totalTimeMinutes").Should().Be(55);
        ReadArray(detail.RootElement, "comments").Should().ContainSingle(comment =>
            ReadString(comment, "content") == "Driver was updated and connectivity was restored.");
        ReadArray(detail.RootElement, "timeEntries").Should().HaveCount(2);

        using var timelineResponse = await customer.GetAsync($"/api/tickets/{ticketNumber}/timeline");
        timelineResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var timeline = JsonDocument.Parse(await timelineResponse.Content.ReadAsStringAsync());
        timeline.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        timeline.RootElement.GetArrayLength().Should().BeGreaterThanOrEqualTo(6);
        var serializedTimeline = timeline.RootElement.GetRawText();
        serializedTimeline.Should().Contain("Resolved");
        serializedTimeline.Should().Contain("Closed");
    }

    [Fact]
    public async Task TicketList_AppliesPaginationSearchFilteringAndSorting()
    {
        using var customer = await factory.CreateAuthenticatedClientAsync(
            "customer@support.local",
            "Customer123!");
        var marker = Guid.NewGuid().ToString("N");

        await CreateTicketAsync(customer, $"First searchable {marker}", "High");
        await CreateTicketAsync(customer, $"Second searchable {marker}", "Low");

        using var response = await customer.GetAsync(
            $"/api/tickets?page=1&pageSize=1&search={marker}&priority=High&status=Open&sortBy=title&sortDirection=asc");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        ReadInt(payload.RootElement, "page").Should().Be(1);
        ReadInt(payload.RootElement, "pageSize").Should().Be(1);
        ReadInt(payload.RootElement, "totalCount").Should().Be(1);
        ReadInt(payload.RootElement, "totalPages").Should().Be(1);
        var item = ReadArray(payload.RootElement, "items").Should().ContainSingle().Subject;
        ReadString(item, "title").Should().Be($"First searchable {marker}");
    }

    [Fact]
    public async Task CreateTicket_WithInvalidInput_ReturnsValidationProblem()
    {
        using var customer = await factory.CreateAuthenticatedClientAsync(
            "customer@support.local",
            "Customer123!");

        using var response = await customer.PostAsJsonAsync("/api/tickets", new
        {
            title = "x",
            description = "short",
            priority = "Medium"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task StatusUpdate_WithInvalidTransition_ReturnsClientError()
    {
        using var customer = await factory.CreateAuthenticatedClientAsync(
            "customer@support.local",
            "Customer123!");
        using var admin = await factory.CreateAuthenticatedClientAsync(
            "admin@support.local",
            "Admin123!");
        var ticketNumber = await CreateTicketAsync(customer);

        using var response = await admin.PatchAsJsonAsync(
            $"/api/tickets/{ticketNumber}/status",
            new { status = "Closed" });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task LogTime_WithoutWorkDate_ReturnsValidationProblem()
    {
        using var customer = await factory.CreateAuthenticatedClientAsync(
            "customer@support.local",
            "Customer123!");
        using var admin = await factory.CreateAuthenticatedClientAsync(
            "admin@support.local",
            "Admin123!");
        using var agent = await factory.CreateAuthenticatedClientAsync(
            "agent@support.local",
            "Agent123!");
        var ticketNumber = await CreateTicketAsync(customer);
        var agentId = await GetSeededAgentIdAsync(admin);
        await ShouldSucceedAsync(admin.PatchAsJsonAsync(
            $"/api/tickets/{ticketNumber}/assignment",
            new { agentId }));

        using var response = await agent.PostAsJsonAsync(
            $"/api/tickets/{ticketNumber}/time-entries",
            new { durationMinutes = 15, description = "Missing date must be rejected" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task LogTime_BeforeTicketCreation_ReturnsBadRequest()
    {
        using var customer = await factory.CreateAuthenticatedClientAsync(
            "customer@support.local",
            "Customer123!");
        using var admin = await factory.CreateAuthenticatedClientAsync(
            "admin@support.local",
            "Admin123!");
        using var agent = await factory.CreateAuthenticatedClientAsync(
            "agent@support.local",
            "Agent123!");
        var ticketNumber = await CreateTicketAsync(customer);
        var agentId = await GetSeededAgentIdAsync(admin);
        await ShouldSucceedAsync(admin.PatchAsJsonAsync(
            $"/api/tickets/{ticketNumber}/assignment",
            new { agentId }));

        using var response = await agent.PostAsJsonAsync(
            $"/api/tickets/{ticketNumber}/time-entries",
            new
            {
                workDate = DateTime.UtcNow.Date.AddDays(-1),
                durationMinutes = 15,
                description = "Impossible historical work"
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ClosedTicket_RejectsNewComments()
    {
        using var customer = await factory.CreateAuthenticatedClientAsync(
            "customer@support.local",
            "Customer123!");
        using var admin = await factory.CreateAuthenticatedClientAsync(
            "admin@support.local",
            "Admin123!");
        using var agent = await factory.CreateAuthenticatedClientAsync(
            "agent@support.local",
            "Agent123!");
        var ticketNumber = await CreateTicketAsync(customer);
        var agentId = await GetSeededAgentIdAsync(admin);
        await ShouldSucceedAsync(admin.PatchAsJsonAsync(
            $"/api/tickets/{ticketNumber}/assignment",
            new { agentId }));
        await ShouldSucceedAsync(agent.PatchAsJsonAsync(
            $"/api/tickets/{ticketNumber}/status",
            new { status = "InProgress" }));
        await ShouldSucceedAsync(agent.PatchAsJsonAsync(
            $"/api/tickets/{ticketNumber}/status",
            new { status = "Resolved" }));
        await ShouldSucceedAsync(customer.PatchAsJsonAsync(
            $"/api/tickets/{ticketNumber}/status",
            new { status = "Closed" }));

        using var response = await customer.PostAsJsonAsync(
            $"/api/tickets/{ticketNumber}/comments",
            new { content = "This must not be stored after closure." });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task<Guid> GetSeededAgentIdAsync(HttpClient admin)
    {
        using var response = await admin.GetAsync("/api/users?role=SupportAgent&activeOnly=true");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var users = payload.RootElement.ValueKind == JsonValueKind.Array
            ? payload.RootElement.EnumerateArray().ToArray()
            : ReadArray(payload.RootElement, "items");
        var agent = users.Single(user => string.Equals(
            ReadString(user, "email"),
            "agent@support.local",
            StringComparison.OrdinalIgnoreCase));
        return Guid.Parse(ReadString(agent, "id"));
    }

    private static Task<string> CreateTicketAsync(HttpClient customer) =>
        CreateTicketAsync(customer, $"Workflow ticket {Guid.NewGuid():N}", "High");

    private static async Task<string> CreateTicketAsync(
        HttpClient customer,
        string title,
        string priority)
    {
        using var response = await customer.PostAsJsonAsync("/api/tickets", new
        {
            title,
            description = "A detailed support request used by the endpoint integration test suite.",
            priority
        });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return ReadString(payload.RootElement, "number");
    }

    private static async Task ShouldSucceedAsync(Task<HttpResponseMessage> responseTask)
    {
        using var response = await responseTask;
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.NoContent);
    }

    private static JsonElement[] ReadArray(JsonElement element, string propertyName)
    {
        ApiTestClientExtensions.TryGetPropertyIgnoringCase(element, propertyName, out var value)
            .Should().BeTrue($"the response should contain '{propertyName}'");
        value.ValueKind.Should().Be(JsonValueKind.Array);
        return value.EnumerateArray().ToArray();
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        ApiTestClientExtensions.TryGetPropertyIgnoringCase(element, propertyName, out var value)
            .Should().BeTrue($"the response should contain '{propertyName}'");
        return value.ValueKind == JsonValueKind.String ? value.GetString()! : value.ToString();
    }

    private static int ReadInt(JsonElement element, string propertyName)
    {
        ApiTestClientExtensions.TryGetPropertyIgnoringCase(element, propertyName, out var value)
            .Should().BeTrue($"the response should contain '{propertyName}'");
        return value.GetInt32();
    }
}
