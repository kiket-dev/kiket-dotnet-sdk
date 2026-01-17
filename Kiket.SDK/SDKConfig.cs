namespace Kiket.SDK;

/// <summary>
/// SDK configuration.
/// </summary>
public class SDKConfig
{
    public string? WebhookSecret { get; set; }
    public string? WorkspaceToken { get; set; }
    public string BaseUrl { get; set; } = "https://kiket.dev";
    public Dictionary<string, object>? Settings { get; set; }
    public string? ExtensionId { get; set; }
    public string? ExtensionVersion { get; set; }
    public string? ManifestPath { get; set; }
    public bool AutoEnvSecrets { get; set; } = true;
    public bool TelemetryEnabled { get; set; } = true;
    public Action<TelemetryRecord>? FeedbackHook { get; set; }
    public string? TelemetryUrl { get; set; }
}
