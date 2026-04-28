using System.Net;
using FluentAssertions;
using NaijaShield.IntegrationTests.Infrastructure;

namespace NaijaShield.IntegrationTests;

[Collection("Integration")]
public class ConversationTests(NaijaShieldWebApplicationFactory factory) : IClassFixture<NaijaShieldWebApplicationFactory>
{
    private readonly HttpClient _client = factory.Client;

    [Fact]
    public async Task GetConversations_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/conversations");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCustomers_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync($"/api/v1/conversations/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
