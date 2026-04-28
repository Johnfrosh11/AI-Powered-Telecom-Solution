using System.Net;
using FluentAssertions;
using NaijaShield.IntegrationTests.Infrastructure;

namespace NaijaShield.IntegrationTests;

/// <summary>
/// Smoke tests for every new controller surface.
/// Verifies that endpoints exist, are protected (401 without auth),
/// and the health check is green — without needing a live database seed.
/// </summary>
[Collection("Integration")]
public class SmokeTests(NaijaShieldWebApplicationFactory factory)
    : IClassFixture<NaijaShieldWebApplicationFactory>
{
    private readonly HttpClient _client = factory.Client;

    // ── Health ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task HealthCheck_Returns200()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Dashboard ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Dashboard_Kpis_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/dashboard/kpis");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Dashboard_TopPatterns_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/dashboard/top-patterns");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Dashboard_LanguageDistribution_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/dashboard/language-distribution");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Users ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Users_List_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/users");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Users_Get_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync($"/api/v1/users/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Roles ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Roles_List_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/roles");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Roles_Get_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync($"/api/v1/roles/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Conversations ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Conversations_List_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/conversations");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Reports ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Reports_List_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/reports");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── AI Studio ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Ai_Prompts_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/ai/prompts");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Audit ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Audit_List_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/audit");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Webhooks ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Webhooks_List_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/webhooks");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Webhooks_InboundCdr_WithoutSignature_Returns401()
    {
        var payload = new StringContent(
            """{"tenantId":"00000000-0000-0000-0000-000000000001","callerMsisdn":"+2348000000001","receiverMsisdn":"+2348000000002","startedAt":"2025-01-01T12:00:00Z","durationSeconds":60}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync("/api/v1/webhooks/inbound/cdr", payload);
        // Missing or invalid signature → Unauthorized
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Swagger ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Swagger_Loads_WithEndpoints()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("/api/v1/dashboard/kpis");
        body.Should().Contain("/api/v1/fraud/calls");
        body.Should().Contain("/api/v1/users");
    }
}
