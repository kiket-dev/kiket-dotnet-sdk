namespace Kiket.SDK;

/// <summary>
/// Context passed to webhook handlers.
/// </summary>
public class HandlerContext
{
    public required string Event { get; init; }
    public required string EventVersion { get; init; }
    public required Dictionary<string, string> Headers { get; init; }
    public required KiketClient Client { get; init; }
    public required ExtensionEndpoints Endpoints { get; init; }
    public required Dictionary<string, object> Settings { get; init; }
    public string? ExtensionId { get; init; }
    public string? ExtensionVersion { get; init; }

    /// <summary>
    /// Secret manager for API-based secret operations.
    /// </summary>
    public required ExtensionSecretManager Secrets { get; init; }

    /// <summary>
    /// Payload secrets (per-org configuration bundled by SecretResolver).
    /// </summary>
    public Dictionary<string, string> PayloadSecrets { get; init; } = new();

    /// <summary>
    /// Retrieves a secret value by key.
    /// <para>
    /// Checks payload secrets first (per-org configuration), then falls back to
    /// environment variables (extension defaults).
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// var slackToken = context.Secret("SLACK_BOT_TOKEN");
    /// // Returns payload.secrets["SLACK_BOT_TOKEN"] ?? Environment.GetEnvironmentVariable("SLACK_BOT_TOKEN")
    /// </code>
    /// </example>
    /// <param name="key">The secret key.</param>
    /// <returns>The secret value, or null if not found.</returns>
    public string? Secret(string key)
    {
        // Payload secrets (per-org) take priority over ENV (extension defaults)
        if (PayloadSecrets.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
        {
            return value;
        }
        return Environment.GetEnvironmentVariable(key);
    }
}
