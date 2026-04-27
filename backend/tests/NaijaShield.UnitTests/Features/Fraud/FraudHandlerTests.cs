using FluentAssertions;
using NSubstitute;
using NaijaShield.Application.Common.Interfaces;
using NaijaShield.Application.Features.Fraud;
using NaijaShield.Domain.Aggregates.ScamDetection;
using Xunit;

namespace NaijaShield.UnitTests.Features.Fraud;

public class IngestCallAudioCommandHandlerTests
{
    private readonly IScamDetectionAiService _aiService = Substitute.For<IScamDetectionAiService>();
    private readonly IScamCallRepository _scamCalls = Substitute.For<IScamCallRepository>();
    private readonly IScamPatternRepository _patterns = Substitute.For<IScamPatternRepository>();
    private readonly IRealtimeNotifier _realtime = Substitute.For<IRealtimeNotifier>();
    private readonly ISmsGateway _sms = Substitute.For<ISmsGateway>();

    private IngestCallAudioCommandHandler CreateHandler() =>
        new(_aiService, _scamCalls, _patterns, _realtime, _sms);

    [Fact]
    public async Task Handle_LowConfidence_ShouldNotSendSms()
    {
        // Arrange
        _aiService.TranscribeAudioAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Transcript text");
        _aiService.DetectLanguageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("en");
        _aiService.ClassifyScamAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ScamClassification(null, 0.4m, "Low confidence match", []));
        _aiService.ExtractEntitiesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ScamEntities([], [], [], [], []));

        var cmd = new IngestCallAudioCommand(
            TenantId: Guid.NewGuid(),
            CallerMsisdn: "+2348012345678",
            ReceiverMsisdn: "+2349087654321",
            StartedAt: DateTime.UtcNow.AddMinutes(-2),
            Duration: TimeSpan.FromMinutes(2),
            AudioStream: new MemoryStream([1, 2, 3]));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _sms.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_HighConfidence_ShouldSendSmsAndNotifyRealtime()
    {
        // Arrange
        _aiService.TranscribeAudioAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Oya send your OTP now or your account go close");
        _aiService.DetectLanguageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("pcm");
        _aiService.TranslateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Send your OTP now or your account will be closed");
        _aiService.ClassifyScamAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ScamClassification(null, 0.95m, "OTP fraud pattern detected", ["OTP", "account close"]));
        _aiService.ExtractEntitiesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ScamEntities([], [], [], [], []));
        _aiService.GenerateWarningSmsAsync(Arg.Any<string>(), Arg.Any<ScamClassification>(), Arg.Any<CancellationToken>())
            .Returns("WARNING: Suspected scam call. Do not share OTP.");
        _sms.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var cmd = new IngestCallAudioCommand(
            TenantId: Guid.NewGuid(),
            CallerMsisdn: "+2348012345678",
            ReceiverMsisdn: "+2349087654321",
            StartedAt: DateTime.UtcNow.AddMinutes(-3),
            Duration: TimeSpan.FromMinutes(3),
            AudioStream: new MemoryStream([1, 2, 3]),
            SuspectedLanguage: "pcm");

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _sms.Received(1).SendAsync(
            "+2349087654321",
            "WARNING: Suspected scam call. Do not share OTP.",
            Arg.Any<CancellationToken>());
        await _realtime.Received(1).NotifyTenantAsync(
            cmd.TenantId,
            "scam.detected",
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }
}
