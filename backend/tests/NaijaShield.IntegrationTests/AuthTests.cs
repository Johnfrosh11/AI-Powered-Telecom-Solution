using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NaijaShield.IntegrationTests.Infrastructure;

namespace NaijaShield.IntegrationTests;

[Collection("Integration")]
public class AuthTests(NaijaShieldWebApplicationFactory factory) : IClassFixture<NaijaShieldWebApplicationFactory>
{
    private readonly HttpClient _client = factory.Client;

    [Fact]
    public async Task Login_WithInvalidCredentials_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "notexist@test.com",
            password = "WrongPass123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithMissingBody_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "",
            password = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Health_Returns200()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
