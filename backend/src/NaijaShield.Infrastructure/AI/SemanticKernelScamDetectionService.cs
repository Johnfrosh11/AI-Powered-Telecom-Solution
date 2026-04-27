using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using NaijaShield.Application.Common.Interfaces;
using System.Text.Json;

namespace NaijaShield.Infrastructure.AI;

/// <summary>
/// Implements the 7-step AI inference pipeline using Semantic Kernel with Azure OpenAI.
/// </summary>
internal sealed class SemanticKernelScamDetectionService(Kernel kernel) : IScamDetectionAiService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<string> TranscribeAudioAsync(Stream audioStream, string language, CancellationToken ct)
    {
        // Azure Cognitive Services Speech SDK — returns transcript text.
        // The kernel plugin wraps the Speech SDK call.
        var fn = kernel.Plugins["SpeechPlugin"]["Transcribe"];
        var args = new KernelArguments
        {
            ["language"] = language
        };

        // Pass stream bytes as base64 for plugin invocation
        using var ms = new MemoryStream();
        await audioStream.CopyToAsync(ms, ct);
        args["audioBase64"] = Convert.ToBase64String(ms.ToArray());

        var result = await kernel.InvokeAsync(fn, args, ct);
        return result.GetValue<string>() ?? string.Empty;
    }

    public async Task<string> DetectLanguageAsync(string text, CancellationToken ct)
    {
        var prompt = $"""
            Detect the language of the following text. 
            Return only the ISO 639-1 code (en, pcm, yo, ha, ig).
            
            Text: {text}
            """;

        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory();
        history.AddUserMessage(prompt);
        var response = await chat.GetChatMessageContentAsync(history, cancellationToken: ct);
        return (response.Content ?? "en").Trim().ToLowerInvariant().Split(' ')[0];
    }

    public async Task<string> TranslateAsync(string text, string fromLanguage, string toLanguage, CancellationToken ct)
    {
        var fn = kernel.Plugins["TranslationPlugin"]["Translate"];
        var result = await kernel.InvokeAsync(fn, new KernelArguments
        {
            ["text"] = text,
            ["fromLanguage"] = fromLanguage,
            ["toLanguage"] = toLanguage
        }, ct);
        return result.GetValue<string>() ?? text;
    }

    public async Task<ScamClassification> ClassifyScamAsync(string transcriptEnglish, string originalLanguage, CancellationToken ct)
    {
        var systemPrompt = """
            You are NaijaShield AI, an expert fraud detection system for Nigerian telecoms.
            Classify the call transcript for scam indicators. Return JSON only:
            {
              "isScam": true/false,
              "confidence": 0.0-1.0,
              "category": "OTP Fraud|SIM Swap|Bank Impersonation|Investment Scam|Romance Scam|Lottery Fraud|Other",
              "severity": "Low|Medium|High|Critical",
              "reasoning": "brief explanation",
              "triggerPhrases": ["phrase1", "phrase2"],
              "matchedPatternId": null
            }
            """;

        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory();
        history.AddSystemMessage(systemPrompt);
        history.AddUserMessage($"Transcript: {transcriptEnglish}\nOriginal language: {originalLanguage}");

        var response = await chat.GetChatMessageContentAsync(history, cancellationToken: ct);
        var json = ExtractJson(response.Content ?? "{}");

        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            return new ScamClassification(
                MatchedPatternId: null,
                Confidence: root.TryGetProperty("confidence", out var conf) ? (decimal)conf.GetDouble() : 0m,
                Reasoning: root.TryGetProperty("reasoning", out var r) ? r.GetString() ?? "" : "",
                TriggerPhrases: root.TryGetProperty("triggerPhrases", out var phrases)
                    ? phrases.EnumerateArray().Select(p => p.GetString() ?? "").ToList()
                    : []);
        }
        catch
        {
            return new ScamClassification(null, 0m, "Parse error", []);
        }
    }

    public async Task<ScamEntities> ExtractEntitiesAsync(string transcript, CancellationToken ct)
    {
        var prompt = $$"""
            Extract named entities from this Nigerian telecom fraud transcript. Return JSON:
            {
              "phoneNumbers": [],
              "bankAccounts": [],
              "personNames": [],
              "organizationNames": [],
              "amounts": []
            }

            Transcript: {{transcript}}
            """;

        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory();
        history.AddUserMessage(prompt);
        var response = await chat.GetChatMessageContentAsync(history, cancellationToken: ct);

        try
        {
            return JsonSerializer.Deserialize<ScamEntities>(
                ExtractJson(response.Content ?? "{}"), JsonOpts) ?? new ScamEntities([], [], [], [], []);
        }
        catch
        {
            return new ScamEntities([], [], [], [], []);
        }
    }

    public async Task<string> GenerateWarningSmsAsync(string language, ScamClassification classification, CancellationToken ct)
    {
        var languageMap = language switch
        {
            "yo" => "Yoruba",
            "ha" => "Hausa",
            "ig" => "Igbo",
            "pcm" => "Nigerian Pidgin English",
            _ => "English"
        };

        var prompt = $"""
            Write a BRIEF (max 160 chars) SMS warning message in {languageMap} to alert a Nigerian mobile user
            they just received a suspected scam call. Category: {classification.Reasoning}.
            Be direct, urgent, and local in tone. Do not mention technical details.
            """;

        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory();
        history.AddUserMessage(prompt);
        var response = await chat.GetChatMessageContentAsync(history, cancellationToken: ct);
        return response.Content ?? "WARNING: Suspected scam call detected. Do not share OTPs or bank details.";
    }

    public async Task<string> GenerateAiSuggestedReplyAsync(string customerMessage, string language, CancellationToken ct)
    {
        var prompt = $"""
            You are a NaijaShield customer support AI. Reply to this customer message in {language}
            in a helpful, empathetic tone. Keep under 300 characters.
            Message: {customerMessage}
            """;

        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory();
        history.AddUserMessage(prompt);
        var response = await chat.GetChatMessageContentAsync(history, cancellationToken: ct);
        return response.Content ?? string.Empty;
    }

    public async Task<string> SummarizeConversationAsync(string conversationContext, CancellationToken ct)
    {
        var prompt = $"Summarize this customer support conversation in 2-3 sentences.\n\n{conversationContext}";

        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory();
        history.AddUserMessage(prompt);
        var response = await chat.GetChatMessageContentAsync(history, cancellationToken: ct);
        return response.Content ?? string.Empty;
    }

    public async Task<string> GenerateExecutiveBriefAsync(DateTime from, DateTime to, Guid tenantId, CancellationToken ct)
    {
        var prompt = $"""
            Generate a 3-paragraph executive brief for a Nigerian telecom fraud report 
            covering {from:yyyy-MM-dd} to {to:yyyy-MM-dd}.
            Include: key findings, top scam categories, regional hotspots, and recommendations.
            Write in professional English suitable for the NCC regulator.
            """;

        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory();
        history.AddUserMessage(prompt);
        var response = await chat.GetChatMessageContentAsync(history, cancellationToken: ct);
        return response.Content ?? string.Empty;
    }

    private static string ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        return start >= 0 && end > start ? text[start..(end + 1)] : "{}";
    }
}
