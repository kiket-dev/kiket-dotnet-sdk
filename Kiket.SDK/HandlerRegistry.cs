namespace Kiket.SDK;

/// <summary>
/// Webhook handler delegate.
/// </summary>
public delegate Task<object?> WebhookHandler(Dictionary<string, object> payload, HandlerContext context);

/// <summary>
/// Handler metadata.
/// </summary>
public record HandlerMetadata(string Event, string Version, WebhookHandler Handler);

/// <summary>
/// Registry for webhook handlers.
/// </summary>
public class HandlerRegistry
{
    private readonly Dictionary<string, HandlerMetadata> _handlers = new();

    public void Register(string eventName, string version, WebhookHandler handler)
    {
        var key = MakeKey(eventName, version);
        _handlers[key] = new HandlerMetadata(eventName, version, handler);
    }

    public HandlerMetadata? Get(string eventName, string version)
    {
        var key = MakeKey(eventName, version);
        return _handlers.TryGetValue(key, out var metadata) ? metadata : null;
    }

    public List<string> EventNames()
    {
        return _handlers.Values.Select(m => m.Event).Distinct().ToList();
    }

    public IEnumerable<HandlerMetadata> All()
    {
        return _handlers.Values;
    }

    private static string MakeKey(string eventName, string version)
    {
        return $"{eventName}:{version}";
    }
}
