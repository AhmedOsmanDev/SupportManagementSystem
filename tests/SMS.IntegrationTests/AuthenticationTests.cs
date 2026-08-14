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
    public async Task Login_WithSeededAdmin_ReturnsJwtAndRefreshTokenAndUserWithoutPasswordData()
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

        ReadRequiredString(payload.RootElement, "accessToken").Should().NotBeNullOrWhiteSpace();
        ReadRequiredString(payload.RootElement, "refreshToken").Should().NotBeNullOrWhiteSpace();
        ApiTestClientExtensions.TryGetPropertyIgnoringCase(payload.RootElement, "refreshTokenExpiresAt", out _)
            .Should().BeTrue();
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

    [Fact]
    public async Task Refresh_WithValidToken_RotatesRefreshTokens_AndRejectsReuse()
    {
        using var client = factory.CreateApiClient();
        var (firstAccessToken, firstRefreshToken) = await LoginForTokensAsync(
            client,
            "customer@support.local",
            "Customer123!");

        var (secondAccessToken, secondRefreshToken) = await RefreshForTokensAsync(client, firstRefreshToken);

        secondAccessToken.Should().NotBe(firstAccessToken);
        secondRefreshToken.Should().NotBe(firstRefreshToken);

        using var reusedResponse = await client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = firstRefreshToken
        });
        reusedResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var revokedDescendantResponse = await client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = secondRefreshToken
        });
        revokedDescendantResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_RevokesTheCurrentRefreshToken()
    {
        using var client = factory.CreateApiClient();
        var (_, refreshToken) = await LoginForTokensAsync(client, "customer@support.local", "Customer123!");

        using var logoutResponse = await client.PostAsJsonAsync("/api/auth/logout", new
        {
            refreshToken
        });
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken
        });
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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

    private static async Task<(string AccessToken, string RefreshToken)> LoginForTokensAsync(
        HttpClient client,
        string email,
        string password)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var payload = JsonDocument.Parse(body);

        return (
            ReadRequiredString(payload.RootElement, "accessToken"),
            ReadRequiredString(payload.RootElement, "refreshToken"));
    }

    private static async Task<(string AccessToken, string RefreshToken)> RefreshForTokensAsync(
        HttpClient client,
        string refreshToken)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var payload = JsonDocument.Parse(body);

        return (
            ReadRequiredString(payload.RootElement, "accessToken"),
            ReadRequiredString(payload.RootElement, "refreshToken"));
    }

    private static string ReadRequiredString(JsonElement element, string propertyName)
    {
        ApiTestClientExtensions.TryGetPropertyIgnoringCase(element, propertyName, out var value)
            .Should().BeTrue($"the payload should contain '{propertyName}'");
        value.ValueKind.Should().Be(JsonValueKind.String);
        value.GetString().Should().NotBeNullOrWhiteSpace();
        return value.GetString()!;
    }
}