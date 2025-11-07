using Xunit;

namespace Kiket.SDK.Tests;

public class HandlerRegistryTests
{
    [Fact]
    public void Register_RegistersHandler()
    {
        var registry = new HandlerRegistry();
        WebhookHandler handler = (payload, context) => Task.FromResult<object?>(new { ok = true });

        registry.Register("test.event", "v1", handler);

        var metadata = registry.Get("test.event", "v1");
        Assert.NotNull(metadata);
        Assert.Equal("test.event", metadata.Event);
        Assert.Equal("v1", metadata.Version);
        Assert.Equal(handler, metadata.Handler);
    }

    [Fact]
    public void Register_AllowsMultipleVersions()
    {
        var registry = new HandlerRegistry();
        WebhookHandler handlerV1 = (payload, context) => Task.FromResult<object?>("v1");
        WebhookHandler handlerV2 = (payload, context) => Task.FromResult<object?>("v2");

        registry.Register("test.event", "v1", handlerV1);
        registry.Register("test.event", "v2", handlerV2);

        Assert.Equal(handlerV1, registry.Get("test.event", "v1")?.Handler);
        Assert.Equal(handlerV2, registry.Get("test.event", "v2")?.Handler);
    }

    [Fact]
    public void Get_ReturnsNullForUnregisteredHandler()
    {
        var registry = new HandlerRegistry();

        var metadata = registry.Get("unknown.event", "v1");

        Assert.Null(metadata);
    }

    [Fact]
    public void EventNames_ReturnsEmptyListWhenNoHandlers()
    {
        var registry = new HandlerRegistry();

        var names = registry.EventNames();

        Assert.Empty(names);
    }

    [Fact]
    public void EventNames_ReturnsUniqueEventNames()
    {
        var registry = new HandlerRegistry();
        registry.Register("event1", "v1", (p, c) => Task.FromResult<object?>(null));
        registry.Register("event1", "v2", (p, c) => Task.FromResult<object?>(null));
        registry.Register("event2", "v1", (p, c) => Task.FromResult<object?>(null));

        var names = registry.EventNames();

        Assert.Equal(2, names.Count);
        Assert.Contains("event1", names);
        Assert.Contains("event2", names);
    }

    [Fact]
    public void All_ReturnsAllHandlers()
    {
        var registry = new HandlerRegistry();
        registry.Register("event1", "v1", (p, c) => Task.FromResult<object?>(null));
        registry.Register("event2", "v1", (p, c) => Task.FromResult<object?>(null));

        var all = registry.All();

        Assert.Equal(2, all.Count());
    }

    [Fact]
    public void Register_OverwritesExistingHandler()
    {
        var registry = new HandlerRegistry();
        WebhookHandler handler1 = (p, c) => Task.FromResult<object?>("first");
        WebhookHandler handler2 = (p, c) => Task.FromResult<object?>("second");

        registry.Register("test.event", "v1", handler1);
        registry.Register("test.event", "v1", handler2);

        Assert.Equal(handler2, registry.Get("test.event", "v1")?.Handler);
    }
}
