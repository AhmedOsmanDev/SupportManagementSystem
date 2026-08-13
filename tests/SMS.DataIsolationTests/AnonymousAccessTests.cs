using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SMS.Testing;

namespace SMS.DataIsolationTests;

public sealed class AnonymousAccessTests(ApiTestFactory factory) : IClassFixture<ApiTestFactory>
{
    [Fact]
    public async Task AnonymousUser_CannotListTickets()
    {
        using var client = factory.CreateApiClient();

        using var response = await client.GetAsync("/api/tickets");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AnonymousUser_CannotReadTicketByManipulatingNumber()
    {
        using var client = factory.CreateApiClient();

        using var response = await client.GetAsync("/api/tickets/TKT-000001");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AnonymousUser_CannotAddCommentByManipulatingNumber()
    {
        using var client = factory.CreateApiClient();

        using var response = await client.PostAsJsonAsync(
            "/api/tickets/TKT-000001/comments",
            new { content = "Unauthorized comment" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

