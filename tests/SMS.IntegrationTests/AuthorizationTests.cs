using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SMS.Testing;

namespace SMS.IntegrationTests;

public sealed class AuthorizationTests(ApiTestFactory factory) : IClassFixture<ApiTestFactory>
{
    [Fact]
    public async Task Customer_CannotOpenAdminDashboard()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(
            "customer@support.local",
            "Customer123!");

        using var response = await client.GetAsync("/api/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Customer_CannotManageUsers()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(
            "customer@support.local",
            "Customer123!");

        using var response = await client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_CanOpenDashboard()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(
            "admin@support.local",
            "Admin123!");

        using var response = await client.GetAsync("/api/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeactivatedUser_ExistingAccessTokenIsImmediatelyRejected()
    {
        using var admin = await factory.CreateAuthenticatedClientAsync(
            "admin@support.local",
            "Admin123!");
        var unique = Guid.NewGuid().ToString("N");
        var email = $"deactivation.{unique}@support.local";
        const string password = "Deactivate123!";

        using var createResponse = await admin.PostAsJsonAsync("/api/users", new
        {
            firstName = "Soon",
            lastName = "Inactive",
            email,
            password,
            role = "Customer"
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        ApiTestClientExtensions.TryGetPropertyIgnoringCase(created.RootElement, "id", out var idElement).Should().BeTrue();
        var userId = Guid.Parse(idElement.GetString()!);

        using var user = await factory.CreateAuthenticatedClientAsync(email, password);
        using var beforeDeactivation = await user.GetAsync("/api/tickets");
        beforeDeactivation.StatusCode.Should().Be(HttpStatusCode.OK);

        using var deactivateResponse = await admin.PatchAsJsonAsync(
            $"/api/users/{userId}/status",
            new { isActive = false });
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var afterDeactivation = await user.GetAsync("/api/tickets");
        afterDeactivation.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
