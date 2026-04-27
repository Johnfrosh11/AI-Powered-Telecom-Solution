using System.Net;
using FluentAssertions;
using NaijaShield.IntegrationTests.Infrastructure;

namespace NaijaShield.IntegrationTests;

[Collection("Integration")]
public class FraudTests(NaijaShieldWebApplicationFactory factory) : IClassFixture<NaijaShieldWebApplicationFactory>
{
    private readonly HttpClient _client = factory.Client;

    [Fact]
    public async Task GetScamCalls_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/fraud/calls");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetScamPatterns_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/fraud/patterns");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetWatchlist_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/fraud/watchlist");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
