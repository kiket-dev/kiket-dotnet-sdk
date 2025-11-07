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
    public required ExtensionSecretManager Secrets { get; init; }
}
