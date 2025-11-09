using Xunit;

namespace Kiket.SDK.Tests;

public class TelemetryReporterTests
{
    [Fact]
    public async Task RecordAsync_DoesNotThrowWhenDisabled()
    {
        var reporter = new TelemetryReporter(false, null, null, "ext-id", "1.0.0", null);

        var exception = await Record.ExceptionAsync(
            () => reporter.RecordAsync("test.event", "v1", "ok", 100)
        );

        Assert.Null(exception);
    }

    [Fact]
    public async Task RecordAsync_DoesNotThrowWhenOptedOut()
    {
        Environment.SetEnvironmentVariable("KIKET_SDK_TELEMETRY_OPTOUT", "1");

        var reporter = new TelemetryReporter(true, null, null, "ext-id", "1.0.0", null);

        var exception = await Record.ExceptionAsync(
            () => reporter.RecordAsync("test.event", "v1", "ok", 100)
        );

        Assert.Null(exception);

        Environment.SetEnvironmentVariable("KIKET_SDK_TELEMETRY_OPTOUT", null);
    }

    [Fact]
    public async Task RecordAsync_CallsFeedbackHookWhenProvided()
    {
        TelemetryRecord? capturedRecord = null;
        void FeedbackHook(TelemetryRecord record) => capturedRecord = record;

        var reporter = new TelemetryReporter(true, null, FeedbackHook, "ext-id", "1.0.0", null);

        await reporter.RecordAsync("test.event", "v1", "ok", 100);

        Assert.NotNull(capturedRecord);
        Assert.Equal("test.event", capturedRecord.Event);
        Assert.Equal("v1", capturedRecord.Version);
        Assert.Equal("ok", capturedRecord.Status);
        Assert.Equal(100, capturedRecord.DurationMs);
        Assert.Equal("ext-id", capturedRecord.ExtensionId);
        Assert.Equal("1.0.0", capturedRecord.ExtensionVersion);
    }

    [Fact]
    public async Task RecordAsync_IncludesErrorMessage()
    {
        TelemetryRecord? capturedRecord = null;
        void FeedbackHook(TelemetryRecord record) => capturedRecord = record;

        var reporter = new TelemetryReporter(true, null, FeedbackHook, "ext-id", "1.0.0", null);

        await reporter.RecordAsync("test.event", "v1", "error", 100, "Handler failed");

        Assert.NotNull(capturedRecord);
        Assert.Equal("error", capturedRecord.Status);
        Assert.Equal("Handler failed", capturedRecord.Message);
    }

    [Fact]
    public async Task RecordAsync_HandlesFailedFeedbackHookGracefully()
    {
        void FeedbackHook(TelemetryRecord record) => throw new Exception("Hook failed");

        var reporter = new TelemetryReporter(true, null, FeedbackHook, "ext-id", "1.0.0", null);

        var exception = await Record.ExceptionAsync(
            () => reporter.RecordAsync("test.event", "v1", "ok", 100)
        );

        Assert.Null(exception);
    }
}
