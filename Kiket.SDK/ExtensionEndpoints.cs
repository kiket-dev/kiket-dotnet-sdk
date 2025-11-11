namespace Kiket.SDK;

/// <summary>
/// High-level extension endpoints.
/// </summary>
public class ExtensionEndpoints
{
    private readonly KiketClient _client;
    private readonly string? _extensionId;
    private readonly string? _eventVersion;

    public ExtensionSecretManager Secrets { get; }

    public ExtensionEndpoints(KiketClient client, string? extensionId, string? eventVersion)
    {
        _client = client;
        _extensionId = extensionId;
        _eventVersion = eventVersion;
        Secrets = new ExtensionSecretManager(client, extensionId);
    }

    public async Task LogEventAsync(string eventName, Dictionary<string, object> data)
    {
        if (string.IsNullOrEmpty(_extensionId))
        {
            throw new InvalidOperationException("Extension ID required for logging events");
        }

        await _client.PostAsync<object>($"/extensions/{_extensionId}/events", new
        {
            @event = eventName,
            version = _eventVersion,
            data,
            timestamp = DateTime.UtcNow
        });
    }

    public async Task<object?> GetMetadataAsync()
    {
        if (string.IsNullOrEmpty(_extensionId))
        {
            throw new InvalidOperationException("Extension ID required for getting metadata");
        }

        return await _client.GetAsync<object>($"/extensions/{_extensionId}");
    }

    public CustomDataClient CustomData(object projectId)
    {
        var value = projectId?.ToString();
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException("projectId is required", nameof(projectId));
        }

        return new CustomDataClient(_client, value);
    }

    public SlaEventsClient SlaEvents(object projectId)
    {
        var value = projectId?.ToString();
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException("projectId is required", nameof(projectId));
        }

        return new SlaEventsClient(_client, value);
    }

    public async Task<RateLimitInfo?> GetRateLimitAsync()
    {
        var response = await _client.GetAsync<RateLimitResponse>("/api/v1/ext/rate_limit");
        return response?.RateLimit;
    }
}
