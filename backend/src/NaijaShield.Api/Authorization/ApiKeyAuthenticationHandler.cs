using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using NaijaShield.Application.Common.Interfaces;

namespace NaijaShield.Api.Authorization;

/// <summary>
/// Authenticates requests carrying an <c>X-Api-Key</c> header.
/// The raw key is SHA-256 hashed and looked up in the database.
/// On success, the resulting principal mirrors a JWT-authenticated user
/// so all downstream permission checks work identically.
/// </summary>
public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IApiKeyRepository apiKeys,
    IUserRepository users)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "ApiKey";
    private const string HeaderName = "X-Api-Key";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var rawKeyValues))
            return AuthenticateResult.NoResult(); // let JWT handler try

        var rawKey = rawKeyValues.ToString();
        if (string.IsNullOrWhiteSpace(rawKey))
            return AuthenticateResult.Fail("Empty API key.");

        var keyHash = ComputeSha256(rawKey);
        var apiKey = await apiKeys.GetByHashAsync(keyHash, Context.RequestAborted);

        if (apiKey is null)
            return AuthenticateResult.Fail("Invalid API key.");

        if (!apiKey.IsValid())
            return AuthenticateResult.Fail("API key is expired or revoked.");

        // Load owning user to hydrate permissions
        var user = await users.GetByIdAsync(apiKey.UserId, Context.RequestAborted);
        if (user is null || !user.IsActive)
            return AuthenticateResult.Fail("API key owner not found or inactive.");

        apiKey.RecordUsage();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, apiKey.UserId.ToString()),
            new("tenant_id", apiKey.TenantId.ToString()),
            new(ClaimTypes.Email, user.Email),
            new("preferred_language", user.PreferredLanguage),
            new("auth_method", "api_key"),
        };

        // Add scopes as permission claims so HasPermission() works
        foreach (var scope in apiKey.Scopes)
            claims.Add(new Claim("permission", scope));

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return AuthenticateResult.Success(ticket);
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
