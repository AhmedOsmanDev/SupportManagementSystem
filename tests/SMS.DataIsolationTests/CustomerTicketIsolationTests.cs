using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SMS.Testing;

namespace SMS.DataIsolationTests;

public sealed class CustomerTicketIsolationTests(ApiTestFactory factory) : IClassFixture<ApiTestFactory>
{
    [Fact]
    public async Task CustomerList_ContainsOwnTicketButExcludesOtherCustomersTicket()
    {
        await using var scenario = await IsolationScenario.CreateAsync(factory);

        using var ownerResponse = await scenario.Owner.GetAsync("/api/tickets?page=1&pageSize=100");
        using var foreignResponse = await scenario.OtherCustomer.GetAsync("/api/tickets?page=1&pageSize=100");

        ownerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        foreignResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadTicketNumbersAsync(ownerResponse)).Should().Contain(scenario.TicketNumber);
        (await ReadTicketNumbersAsync(foreignResponse)).Should().NotContain(scenario.TicketNumber);
    }

    [Fact]
    public async Task CustomerDetail_ForAnotherCustomersKnownTicketNumber_ReturnsNotFound()
    {
        await using var scenario = await IsolationScenario.CreateAsync(factory);

        using var response = await scenario.OtherCustomer.GetAsync(
            $"/api/tickets/{scenario.TicketNumber}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "a resource probe must not reveal that the foreign ticket exists");
    }

    [Fact]
    public async Task CustomerComment_ForAnotherCustomersKnownTicketNumber_IsRejectedAndNotPersisted()
    {
        await using var scenario = await IsolationScenario.CreateAsync(factory);
        var marker = $"forbidden-comment-{Guid.NewGuid():N}";

        using var forbiddenResponse = await scenario.OtherCustomer.PostAsJsonAsync(
            $"/api/tickets/{scenario.TicketNumber}/comments",
            new { content = marker });

        forbiddenResponse.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "the API should reject the write without disclosing ownership information");

        using var ownerResponse = await scenario.Owner.GetAsync(
            $"/api/tickets/{scenario.TicketNumber}");
        ownerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var ownerBody = await ownerResponse.Content.ReadAsStringAsync();
        ownerBody.Should().NotContain(marker);
    }

    [Fact]
    public async Task CustomerStatusUpdate_ForAnotherCustomersTicket_ReturnsNotFound()
    {
        await using var scenario = await IsolationScenario.CreateAsync(factory);

        using var response = await scenario.OtherCustomer.PatchAsJsonAsync(
            $"/api/tickets/{scenario.TicketNumber}/status",
            new { status = "Closed" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CustomerTimeline_ForAnotherCustomersTicket_ReturnsNotFound()
    {
        await using var scenario = await IsolationScenario.CreateAsync(factory);

        using var response = await scenario.OtherCustomer.GetAsync(
            $"/api/tickets/{scenario.TicketNumber}/timeline");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateTicket_IgnoresAttemptedCustomerIdOverpostingAndUsesJwtIdentity()
    {
        using var owner = await factory.CreateAuthenticatedClientAsync(
            "customer@support.local",
            "Customer123!");
        var attackerSuppliedCustomerId = Guid.NewGuid();

        using var createResponse = await owner.PostAsJsonAsync("/api/tickets", new
        {
            title = $"Ownership overpost {Guid.NewGuid():N}",
            description = "The customer identity in this request must come only from the validated JWT.",
            priority = "Medium",
            customerId = attackerSuppliedCustomerId
        });

        createResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);
        using var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        ReadString(created.RootElement, "customerId").Should().NotBe(
            attackerSuppliedCustomerId.ToString(),
            "the supplied customerId must never override the authenticated identity");
    }

    private static async Task<IReadOnlyCollection<int>> ReadTicketNumbersAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        ApiTestClientExtensions.TryGetPropertyIgnoringCase(document.RootElement, "items", out var items)
            .Should().BeTrue();

        return items.EnumerateArray()
            .Select(item => ReadInt(item, "number"))
            .ToArray();
    }

    private static int ReadInt(JsonElement element, string propertyName)
    {
        ApiTestClientExtensions.TryGetPropertyIgnoringCase(element, propertyName, out var value)
            .Should().BeTrue($"the response should contain '{propertyName}'");
        return value.GetInt32();
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        ApiTestClientExtensions.TryGetPropertyIgnoringCase(element, propertyName, out var value)
            .Should().BeTrue($"the response should contain '{propertyName}'");
        return value.ValueKind == JsonValueKind.String ? value.GetString()! : value.ToString();
    }

}
