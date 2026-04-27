using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using NaijaShield.Application.Common.Interfaces;

namespace NaijaShield.Infrastructure.Integrations;

// ── Africa's Talking SMS Gateway ──────────────────────────────────────────────

internal sealed class AfricasTalkingGateway(
    System.Net.Http.IHttpClientFactory httpClientFactory,
    IConfiguration config) : ISmsGateway
{
    public async Task<bool> SendAsync(string msisdn, string message, CancellationToken ct)
    {
        var apiKey = config["AfricasTalking:ApiKey"];
        var username = config["AfricasTalking:Username"];
        var from = config["AfricasTalking:SenderId"] ?? "NaijaShield";

        var client = httpClientFactory.CreateClient("AfricasTalking");

        var content = new System.Net.Http.FormUrlEncodedContent(
        [
            new("username", username ?? string.Empty),
            new("to", msisdn),
            new("message", message),
            new("from", from)
        ]);

        var response = await client.PostAsync("https://api.africastalking.com/version1/messaging", content, ct);
        return response.IsSuccessStatusCode;
    }
}

// ── WhatsApp Business Cloud API Gateway ──────────────────────────────────────

internal sealed class WhatsAppBusinessGateway(
    System.Net.Http.IHttpClientFactory httpClientFactory,
    IConfiguration config) : IWhatsAppGateway
{
    public async Task<bool> SendAsync(string msisdn, string message, CancellationToken ct)
    {
        var token = config["WhatsApp:AccessToken"];
        var phoneId = config["WhatsApp:PhoneNumberId"];

        var client = httpClientFactory.CreateClient("WhatsApp");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var body = System.Text.Json.JsonSerializer.Serialize(new
        {
            messaging_product = "whatsapp",
            to = msisdn.TrimStart('+'),
            type = "text",
            text = new { body = message }
        });

        var response = await client.PostAsync(
            $"https://graph.facebook.com/v20.0/{phoneId}/messages",
            new System.Net.Http.StringContent(body, System.Text.Encoding.UTF8, "application/json"), ct);
        return response.IsSuccessStatusCode;
    }
}

// ── Azure Blob Storage ────────────────────────────────────────────────────────

internal sealed class AzureBlobStorageService(BlobServiceClient blobServiceClient) : IAzureBlobStorage
{
    public async Task<string> UploadAsync(
        Stream content, string containerName, string blobName,
        string contentType, CancellationToken ct)
    {
        var container = blobServiceClient.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(cancellationToken: ct);

        var blob = container.GetBlobClient(blobName);
        await blob.UploadAsync(content, new Azure.Storage.Blobs.Models.BlobUploadOptions
        {
            HttpHeaders = new Azure.Storage.Blobs.Models.BlobHttpHeaders { ContentType = contentType }
        }, ct);

        return blob.Uri.ToString();
    }

    public async Task<Stream> DownloadAsync(string containerName, string blobName, CancellationToken ct)
    {
        var blob = blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName);
        var response = await blob.DownloadStreamingAsync(cancellationToken: ct);
        return response.Value.Content;
    }

    public async Task DeleteAsync(string containerName, string blobName, CancellationToken ct)
    {
        var blob = blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName);
        await blob.DeleteIfExistsAsync(cancellationToken: ct);
    }

    public string GetBlobUrl(string containerName, string blobName)
    {
        return blobServiceClient.GetBlobContainerClient(containerName)
            .GetBlobClient(blobName).Uri.ToString();
    }
}

// ── Azure Service Bus ─────────────────────────────────────────────────────────

internal sealed class AzureServiceBusService(ServiceBusClient serviceBusClient) : IAzureServiceBus
{
    public async Task PublishAsync<T>(string topicName, T message, CancellationToken ct)
    {
        var sender = serviceBusClient.CreateSender(topicName);
        var json = System.Text.Json.JsonSerializer.Serialize(message);
        await sender.SendMessageAsync(new ServiceBusMessage(json), ct);
    }

    public async Task PublishBatchAsync<T>(string topicName, IEnumerable<T> messages, CancellationToken ct)
    {
        var sender = serviceBusClient.CreateSender(topicName);
        var batch = await sender.CreateMessageBatchAsync(ct);
        foreach (var msg in messages)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(msg);
            batch.TryAddMessage(new ServiceBusMessage(json));
        }
        await sender.SendMessagesAsync(batch, ct);
    }
}

// ── Government Regulatory Stubs ───────────────────────────────────────────────
// Real integrations would authenticate via mutual TLS or OAuth, per NCC/CBN/EFCC specifications.

internal sealed class NccReportingClient(
    System.Net.Http.IHttpClientFactory httpClientFactory,
    IConfiguration config) : INccReportingClient
{
    public async Task<string> SubmitAsync(string reportJson, CancellationToken ct)
    {
        var baseUrl = config["NCC:BaseUrl"] ?? "https://ncc-reporting-stub.azure-api.net";
        var apiKey = config["NCC:ApiKey"] ?? string.Empty;

        var client = httpClientFactory.CreateClient("NCC");
        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);

        var response = await client.PostAsync(
            $"{baseUrl}/api/fraud-reports",
            new System.Net.Http.StringContent(reportJson, System.Text.Encoding.UTF8, "application/json"), ct);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync(ct);
        var doc = System.Text.Json.JsonDocument.Parse(result);
        return doc.RootElement.TryGetProperty("reference", out var r) ? r.GetString() ?? "NCC-REF" : "NCC-REF";
    }
}

internal sealed class CbnAntiFraudClient : ICbnAntiFraudClient
{
    public Task<string> SubmitAsync(string reportJson, CancellationToken ct) =>
        Task.FromResult("CBN-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant());
}

internal sealed class EfccReportingClient : IEfccReportingClient
{
    public Task<string> SubmitAsync(string reportJson, CancellationToken ct) =>
        Task.FromResult("EFCC-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant());
}

internal sealed class NimcNinClient : INimcNinClient
{
    public Task<bool> VerifyAsync(string nin, string msisdn, CancellationToken ct) =>
        Task.FromResult(true);
}
