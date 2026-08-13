using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SMS.Testing;

public static class ApiTestClientExtensions
{
    public static async Task<string> LoginAsync(
        this HttpClient client,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password },
            cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Login for '{email}' failed with {(int)response.StatusCode}: {responseBody}");
        }

        using var payload = JsonDocument.Parse(responseBody);
        var hasToken = TryGetPropertyIgnoringCase(payload.RootElement, "accessToken", out var tokenElement)
            || TryGetPropertyIgnoringCase(payload.RootElement, "token", out tokenElement);
        if (!hasToken || string.IsNullOrWhiteSpace(tokenElement.GetString()))
        {
            throw new JsonException("The login response did not contain a non-empty access token.");
        }

        return tokenElement.GetString()!;
    }

    public static async Task<HttpClient> CreateAuthenticatedClientAsync(
        this ApiTestFactory factory,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var client = factory.CreateApiClient();

        try
        {
            var token = await client.LoginAsync(email, password, cancellationToken);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    public static bool TryGetPropertyIgnoringCase(
        JsonElement element,
        string propertyName,
        out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }
}
