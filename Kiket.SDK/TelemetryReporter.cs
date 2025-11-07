using System.Net.Http.Json;

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
    string? ExtensionId,
    string? ExtensionVersion,
    DateTime Timestamp
);

/// <summary>
/// Telemetry reporter for SDK usage metrics.
/// </summary>
public class TelemetryReporter
{
    private readonly bool _enabled;
    private readonly HttpClient? _httpClient;
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
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(telemetryUrl),
                Timeout = TimeSpan.FromSeconds(5)
            };
        }
    }

    public async Task RecordAsync(string eventName, string version, string status, double durationMs, string? message = null)
    {
        if (!_enabled) return;

        var record = new TelemetryRecord(
            eventName,
            version,
            status,
            durationMs,
            message,
            _extensionId,
            _extensionVersion,
            DateTime.UtcNow
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
        if (_httpClient != null)
        {
            try
            {
                await _httpClient.PostAsJsonAsync("/telemetry", record);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send telemetry: {ex.Message}");
            }
        }
    }
}
