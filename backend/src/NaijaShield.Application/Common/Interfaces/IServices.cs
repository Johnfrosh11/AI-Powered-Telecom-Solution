namespace NaijaShield.Application.Common.Interfaces;

/// <summary>Hashes and verifies passwords.</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

/// <summary>Sends an SMS message via the configured gateway (Africa's Talking / Twilio).</summary>
public interface ISmsGateway
{
    Task<bool> SendAsync(string to, string message, CancellationToken ct = default);
}

/// <summary>Sends a WhatsApp message via WhatsApp Business API.</summary>
public interface IWhatsAppGateway
{
    Task<bool> SendAsync(string to, string message, CancellationToken ct = default);
}

/// <summary>Transcribes audio to text using Azure Speech Services.</summary>
public interface IAzureSpeechClient
{
    Task<string> TranscribeAsync(Stream audio, string suspectedLanguage, CancellationToken ct = default);
    Task<string> DetectLanguageAsync(string text, CancellationToken ct = default);
}

/// <summary>Translates text via Azure Translator.</summary>
public interface IAzureTranslatorClient
{
    Task<string> TranslateAsync(string text, string sourceLang, string targetLang, CancellationToken ct = default);
}

/// <summary>Uploads / downloads blobs from Azure Blob Storage.</summary>
public interface IAzureBlobStorage
{
    Task<string> UploadAsync(Stream content, string containerName, string blobName, string contentType, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string containerName, string blobName, CancellationToken ct = default);
    Task DeleteAsync(string containerName, string blobName, CancellationToken ct = default);
    string GetBlobUrl(string containerName, string blobName);
}

/// <summary>Publishes messages to Azure Service Bus.</summary>
public interface IAzureServiceBus
{
    Task PublishAsync<T>(string topicName, T message, CancellationToken ct = default);
    Task PublishBatchAsync<T>(string topicName, IEnumerable<T> messages, CancellationToken ct = default);
}

/// <summary>Reads a secret from Azure Key Vault.</summary>
public interface IAzureKeyVaultClient
{
    Task<string> GetSecretAsync(string secretName, CancellationToken ct = default);
    Task SetSecretAsync(string secretName, string value, CancellationToken ct = default);
}

/// <summary>Writes audit log entries, chaining HMAC hashes for tamper evidence.</summary>
public interface IAuditLogger
{
    Task LogAsync(
        Guid tenantId,
        Guid? userId,
        string actorType,
        string action,
        string targetType,
        string targetId,
        bool success,
        string sensitivity = "Low",
        object? metadata = null,
        CancellationToken ct = default);
}

/// <summary>Resolves and caches the effective PermissionEntry set for a user.</summary>
public interface IPermissionCache
{
    Task<IReadOnlyList<string>> GetPermissionsAsync(Guid userId, CancellationToken ct = default);
    Task SetAsync(Guid userId, IReadOnlyList<string> permissions, CancellationToken ct = default);
    Task InvalidateAsync(Guid userId);
}

/// <summary>Sends real-time events to frontend clients via SignalR.</summary>
public interface IRealtimeNotifier
{
    Task NotifyTenantAsync(Guid tenantId, string eventName, object payload, CancellationToken ct = default);
    Task NotifyUserAsync(Guid userId, string eventName, object payload, CancellationToken ct = default);
}

/// <summary>Issues and validates JWT access tokens.</summary>
public interface ITokenService
{
    string GenerateAccessToken(TokenClaims claims);
    string GenerateRefreshToken();
    TokenClaims? ValidateAccessToken(string token);
}

/// <summary>Claims embedded in a JWT.</summary>
public record TokenClaims(
    Guid UserId,
    Guid TenantId,
    string Email,
    string PreferredLanguage,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

/// <summary>Evaluates NaijaShield feature flags.</summary>
public interface IFeatureFlagService
{
    Task<bool> IsEnabledAsync(string key, Guid tenantId, CancellationToken ct = default);
}

/// <summary>Submits a report to EFCC's API.</summary>
public interface IEfccReportingClient
{
    Task<string> SubmitAsync(string reportJson, CancellationToken ct = default);
}

/// <summary>Submits a report to CBN's anti-fraud portal.</summary>
public interface ICbnAntiFraudClient
{
    Task<string> SubmitAsync(string reportJson, CancellationToken ct = default);
}

/// <summary>Submits quality-of-service / complaint reports to NCC.</summary>
public interface INccReportingClient
{
    Task<string> SubmitAsync(string reportJson, CancellationToken ct = default);
}

/// <summary>Verifies NIN–SIM linkage via NIMC.</summary>
public interface INimcNinClient
{
    Task<bool> VerifyAsync(string nin, string msisdn, CancellationToken ct = default);
}
