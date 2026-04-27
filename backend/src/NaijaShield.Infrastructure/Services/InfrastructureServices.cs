using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NaijaShield.Application.Common.Interfaces;
using NaijaShield.Domain.Aggregates.Audit;
using NaijaShield.Domain.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NaijaShield.Infrastructure.Services;

// ── CurrentUserService ────────────────────────────────────────────────────────

internal sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid? UserId => Guid.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    public Guid? TenantId => Guid.TryParse(User?.FindFirstValue("tenant_id"), out var id) ? id : null;
    public string? Email => User?.FindFirstValue(ClaimTypes.Email);
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
    public IReadOnlyList<string> Roles => User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? [];
    public IReadOnlyList<string> Permissions => User?.FindAll("PermissionEntry").Select(c => c.Value).ToList() ?? [];
    public bool HasPermission(string PermissionEntry) => Permissions.Contains(PermissionEntry);
}

// ── TokenService ──────────────────────────────────────────────────────────────

internal sealed class TokenService(Microsoft.Extensions.Configuration.IConfiguration config) : ITokenService
{
    private string SecretKey => config["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT secret key not configured.");
    private string Issuer => config["Jwt:Issuer"] ?? "naijashield";
    private string Audience => config["Jwt:Audience"] ?? "naijashield-api";

    public string GenerateAccessToken(TokenClaims claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claimsList = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, claims.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, claims.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("tenant_id", claims.TenantId.ToString()),
            new("preferred_language", claims.PreferredLanguage),
        };

        foreach (var role in claims.Roles)
            claimsList.Add(new Claim(ClaimTypes.Role, role));
        foreach (var perm in claims.Permissions)
            claimsList.Add(new Claim("PermissionEntry", perm));

        var expires = DateTime.UtcNow.AddMinutes(int.Parse(config["Jwt:ExpiryMinutes"] ?? "60"));
        var token = new JwtSecurityToken(Issuer, Audience, claimsList, expires: expires, signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public TokenClaims? ValidateAccessToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = Issuer,
                ValidateAudience = true,
                ValidAudience = Audience,
                ClockSkew = TimeSpan.Zero
            }, out _);

            var userId = Guid.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? Guid.Empty.ToString());
            var tenantId = Guid.Parse(principal.FindFirstValue("tenant_id") ?? Guid.Empty.ToString());
            var email = principal.FindFirstValue(JwtRegisteredClaimNames.Email) ?? string.Empty;
            var lang = principal.FindFirstValue("preferred_language") ?? "en";
            var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            var perms = principal.FindAll("PermissionEntry").Select(c => c.Value).ToList();

            return new TokenClaims(userId, tenantId, email, lang, roles, perms);
        }
        catch
        {
            return null;
        }
    }
}

// ── AuditLoggerService ────────────────────────────────────────────────────────

internal sealed class AuditLoggerService(
    NaijaShield.Infrastructure.Persistence.AppDbContext db,
    IHttpContextAccessor httpContextAccessor) : IAuditLogger
{
    public async Task LogAsync(
        Guid tenantId,
        Guid? userId,
        string actorType,
        string action,
        string targetType,
        string targetId,
        bool success,
        string sensitivity = "Low",
        object? metadata = null,
        CancellationToken ct = default)
    {
        var ip = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ua = httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString() ?? "unknown";

        var prevHash = await GetLastChainHashAsync(tenantId, ct);
        var chainInput = $"{prevHash}{userId}{action}{DateTime.UtcNow:O}";
        var chainHash = ComputeHmac(chainInput);

        Enum.TryParse<AuditSensitivity>(sensitivity, ignoreCase: true, out var sens);

        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            ActorType = actorType,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            IpAddress = ip,
            UserAgent = ua,
            Result = success ? AuditResult.Success : AuditResult.Failure,
            Sensitivity = sens,
            MetadataJson = metadata is null ? "{}" : JsonSerializer.Serialize(metadata),
            OccurredAt = DateTime.UtcNow,
            ChainHash = chainHash,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.AuditLogs.Add(log);
        await db.SaveChangesAsync(ct);
    }

    private async Task<string> GetLastChainHashAsync(Guid tenantId, CancellationToken ct)
    {
        var last = await db.AuditLogs
            .Where(a => a.TenantId == tenantId)
            .OrderByDescending(a => a.OccurredAt)
            .Select(a => a.ChainHash)
            .FirstOrDefaultAsync(ct);
        return last ?? "genesis";
    }

    private static string ComputeHmac(string input)
    {
        var keyBytes = Encoding.UTF8.GetBytes("naijashield-audit-key");
        using var hmac = new HMACSHA256(keyBytes);
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(input)));
    }
}

// ── PermissionCacheService ────────────────────────────────────────────────────

internal sealed class PermissionCacheService(
    IDistributedCache cache,
    ILogger<PermissionCacheService> logger) : IPermissionCache
{
    private static string Key(Guid userId) => $"permissions:{userId}";

    public async Task<IReadOnlyList<string>> GetPermissionsAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var json = await cache.GetStringAsync(Key(userId), ct);
            if (json is null) return [];
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read permissions from cache for user {UserId}", userId);
            return [];
        }
    }

    public async Task SetAsync(Guid userId, IReadOnlyList<string> permissions, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(permissions);
            await cache.SetStringAsync(Key(userId), json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            }, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to write permissions to cache for user {UserId}", userId);
        }
    }

    public Task InvalidateAsync(Guid userId) =>
        cache.RemoveAsync(Key(userId));
}

// ── FeatureFlagService ────────────────────────────────────────────────────────

internal sealed class FeatureFlagService(
    NaijaShield.Infrastructure.Persistence.AppDbContext db) : IFeatureFlagService
{
    public async Task<bool> IsEnabledAsync(string key, Guid tenantId, CancellationToken ct)
    {
        var flag = await db.FeatureFlags
            .FirstOrDefaultAsync(f => f.Key == key &&
                (f.TenantId == tenantId || f.TenantId == Guid.Empty), ct);
        return flag?.IsEnabled ?? false;
    }
}
