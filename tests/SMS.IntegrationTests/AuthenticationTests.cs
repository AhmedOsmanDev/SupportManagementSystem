using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SMS.Testing;

namespace SMS.IntegrationTests;

public sealed class AuthenticationTests(ApiTestFactory factory) : IClassFixture<ApiTestFactory>
{
    [Fact]
    public async Task Login_WithSeededAdmin_ReturnsJwtAndUserWithoutPasswordData()
    {
        using var client = factory.CreateApiClient();

        using var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@support.local",
            password = "Admin123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var payload = JsonDocument.Parse(body);

        ApiTestClientExtensions.TryGetPropertyIgnoringCase(payload.RootElement, "accessToken", out var token)
            .Should().BeTrue();
        token.GetString().Should().NotBeNullOrWhiteSpace();
        body.Contains("passwordHash", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
        body.Contains("Admin123!", StringComparison.Ordinal).Should().BeFalse();
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        using var client = factory.CreateApiClient();

        using var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@support.local",
            password = "incorrect-password"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/api/auth/me")]
    [InlineData("/api/tickets")]
    [InlineData("/api/dashboard")]
    [InlineData("/api/users")]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized(string endpoint)
    {
        using var client = factory.CreateApiClient();

        using var response = await client.GetAsync(endpoint);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithMalformedToken_ReturnsUnauthorized()
    {
        using var client = factory.CreateApiClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "not-a-valid-jwt");

        using var response = await client.GetAsync("/api/tickets");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CurrentUser_WithValidToken_ReturnsMatchingIdentity()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(
            "customer@support.local",
            "Customer123!");

        using var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Contains("customer@support.local", StringComparison.OrdinalIgnoreCase).Should().BeTrue();
        body.Contains("password", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
    }
}
