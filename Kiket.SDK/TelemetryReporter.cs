using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace Kiket.SDK;

/// <summary>
/// Telemetry record.
/// </summary>
public record TelemetryRecord(
    string Event,
    string Version,
    string Status,
    double DurationMs,
    string? Message,
    string? ErrorClass,
    string? ExtensionId,
    string? ExtensionVersion,
    DateTime Timestamp,
    Dictionary<string, object?>? Metadata = null
);

/// <summary>
/// Telemetry reporter for SDK usage metrics.
/// </summary>
public class TelemetryReporter
{
    private readonly bool _enabled;
    private readonly HttpClient? _httpClient;
    private readonly string? _endpoint;
    private readonly Action<TelemetryRecord>? _feedbackHook;
    private readonly string? _extensionId;
    private readonly string? _extensionVersion;

    public TelemetryReporter(
        bool enabled,
        string? telemetryUrl,
        Action<TelemetryRecord>? feedbackHook,
        string? extensionId,
        string? extensionVersion)
    {
        var optOut = Environment.GetEnvironmentVariable("KIKET_SDK_TELEMETRY_OPTOUT") == "1";
        _enabled = enabled && !optOut;
        _feedbackHook = feedbackHook;
        _extensionId = extensionId;
        _extensionVersion = extensionVersion;

        if (!string.IsNullOrEmpty(telemetryUrl))
        {
            _endpoint = ResolveEndpoint(telemetryUrl);
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        }
    }

    public async Task RecordAsync(string eventName, string version, string status, double durationMs, string? message = null, string? errorClass = null, Dictionary<string, object?>? metadata = null)
    {
        if (!_enabled) return;

        var record = new TelemetryRecord(
            eventName,
            version,
            status,
            durationMs,
            message,
            errorClass,
            _extensionId,
            _extensionVersion,
            DateTime.UtcNow,
            metadata
        );

        // Call feedback hook
        if (_feedbackHook != null)
        {
            try
            {
                _feedbackHook(record);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Feedback hook failed: {ex.Message}");
            }
        }

        // Send to telemetry URL
        if (_httpClient != null && !string.IsNullOrEmpty(_endpoint))
        {
            try
            {
                var payload = BuildPayload(record);
                using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
                {
                    Content = JsonContent.Create(payload, options: new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = null
                    })
                };
                await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send telemetry: {ex.Message}");
            }
        }
    }

    private static string ResolveEndpoint(string telemetryUrl)
    {
        var trimmed = telemetryUrl.TrimEnd('/');
        return trimmed.EndsWith("/telemetry", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : $"{trimmed}/telemetry";
    }

    private static Dictionary<string, object?> BuildPayload(TelemetryRecord record)
    {
        var metadata = record.Metadata ?? new Dictionary<string, object?>();
        metadata = new Dictionary<string, object?>(metadata);

        return new Dictionary<string, object?>
        {
            ["event"] = record.Event,
            ["version"] = record.Version,
            ["status"] = record.Status,
            ["duration_ms"] = Math.Round(record.DurationMs),
            ["timestamp"] = record.Timestamp.ToString("o"),
            ["extension_id"] = record.ExtensionId,
            ["extension_version"] = record.ExtensionVersion,
            ["error_message"] = record.Message,
            ["error_class"] = record.ErrorClass,
            ["metadata"] = metadata
        };
    }
}
