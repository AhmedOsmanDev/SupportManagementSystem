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
            $"/api/tickets/{Uri.EscapeDataString(scenario.TicketNumber)}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "a resource probe must not reveal that the foreign ticket exists");
    }

    [Fact]
    public async Task CustomerComment_ForAnotherCustomersKnownTicketNumber_IsRejectedAndNotPersisted()
    {
        await using var scenario = await IsolationScenario.CreateAsync(factory);
        var marker = $"forbidden-comment-{Guid.NewGuid():N}";

        using var forbiddenResponse = await scenario.OtherCustomer.PostAsJsonAsync(
            $"/api/tickets/{Uri.EscapeDataString(scenario.TicketNumber)}/comments",
            new { content = marker });

        forbiddenResponse.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "the API should reject the write without disclosing ownership information");

        using var ownerResponse = await scenario.Owner.GetAsync(
            $"/api/tickets/{Uri.EscapeDataString(scenario.TicketNumber)}");
        ownerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var ownerBody = await ownerResponse.Content.ReadAsStringAsync();
        ownerBody.Should().NotContain(marker);
    }

    [Fact]
    public async Task CustomerStatusUpdate_ForAnotherCustomersTicket_ReturnsNotFound()
    {
        await using var scenario = await IsolationScenario.CreateAsync(factory);

        using var response = await scenario.OtherCustomer.PatchAsJsonAsync(
            $"/api/tickets/{Uri.EscapeDataString(scenario.TicketNumber)}/status",
            new { status = "Closed" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CustomerTimeline_ForAnotherCustomersTicket_ReturnsNotFound()
    {
        await using var scenario = await IsolationScenario.CreateAsync(factory);

        using var response = await scenario.OtherCustomer.GetAsync(
            $"/api/tickets/{Uri.EscapeDataString(scenario.TicketNumber)}/timeline");

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

    private static async Task<IReadOnlyCollection<string>> ReadTicketNumbersAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        ApiTestClientExtensions.TryGetPropertyIgnoringCase(document.RootElement, "items", out var items)
            .Should().BeTrue();

        return items.EnumerateArray()
            .Select(item => ReadString(item, "number"))
            .Where(number => !string.IsNullOrWhiteSpace(number))
            .ToArray();
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        ApiTestClientExtensions.TryGetPropertyIgnoringCase(element, propertyName, out var value)
            .Should().BeTrue($"the response should contain '{propertyName}'");
        return value.ValueKind == JsonValueKind.String ? value.GetString()! : value.ToString();
    }

    private sealed class IsolationScenario : IAsyncDisposable
    {
        private IsolationScenario(HttpClient owner, HttpClient otherCustomer, string ticketNumber)
        {
            Owner = owner;
            OtherCustomer = otherCustomer;
            TicketNumber = ticketNumber;
        }

        public HttpClient Owner { get; }
        public HttpClient OtherCustomer { get; }
        public string TicketNumber { get; }

        public static async Task<IsolationScenario> CreateAsync(ApiTestFactory factory)
        {
            var owner = await factory.CreateAuthenticatedClientAsync(
                "customer@support.local",
                "Customer123!");

            try
            {
                var ticketNumber = await CreateOwnedTicketAsync(owner);
                var otherCustomer = await CreateOtherCustomerAsync(factory);
                return new IsolationScenario(owner, otherCustomer, ticketNumber);
            }
            catch
            {
                owner.Dispose();
                throw;
            }
        }

        private static async Task<string> CreateOwnedTicketAsync(HttpClient owner)
        {
            using var response = await owner.PostAsJsonAsync("/api/tickets", new
            {
                title = $"Isolation ticket {Guid.NewGuid():N}",
                description = "Only the authenticated owner is permitted to read or modify this ticket.",
                priority = "High"
            });

            var body = await response.Content.ReadAsStringAsync();
            response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);
            using var payload = JsonDocument.Parse(body);
            return ReadString(payload.RootElement, "number");
        }

        private static async Task<HttpClient> CreateOtherCustomerAsync(ApiTestFactory factory)
        {
            var unique = Guid.NewGuid().ToString("N");
            var email = $"isolation.{unique}@support.local";
            const string password = "CustomerTwo123!";
            using var admin = await factory.CreateAuthenticatedClientAsync(
                "admin@support.local",
                "Admin123!");

            using var createResponse = await admin.PostAsJsonAsync("/api/users", new
            {
                firstName = "Isolation",
                lastName = "Customer",
                email,
                password,
                role = "Customer"
            });

            createResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);
            return await factory.CreateAuthenticatedClientAsync(email, password);
        }

        public ValueTask DisposeAsync()
        {
            Owner.Dispose();
            OtherCustomer.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
