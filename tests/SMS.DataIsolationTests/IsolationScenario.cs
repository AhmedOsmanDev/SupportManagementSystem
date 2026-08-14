using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SMS.Testing;

namespace SMS.DataIsolationTests;

internal sealed class IsolationScenario : IAsyncDisposable
{
    private IsolationScenario(HttpClient owner, HttpClient otherCustomer, int ticketNumber)
    {
        Owner = owner;
        OtherCustomer = otherCustomer;
        TicketNumber = ticketNumber;
    }

    public HttpClient Owner { get; }
    public HttpClient OtherCustomer { get; }
    public int TicketNumber { get; }

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

    private static async Task<int> CreateOwnedTicketAsync(HttpClient owner)
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
        return ReadInt(payload.RootElement, "number");
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

    public ValueTask DisposeAsync()
    {
        Owner.Dispose();
        OtherCustomer.Dispose();
        return ValueTask.CompletedTask;
    }
}
