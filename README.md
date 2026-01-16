# Kiket .NET SDK

> Build and run Kiket extensions with a batteries-included, strongly-typed .NET toolkit.

## Features

- 🔌 **Webhook handlers** – register handlers for events with `sdk.Register("issue.created", "v1", handler)`.
- 🔐 **Transparent authentication** – HMAC verification for inbound payloads, workspace-token client for outbound calls.
- 🔑 **Secret manager** – list, fetch, rotate, and delete extension secrets stored in Google Secret Manager.
- 🌐 **Built-in ASP.NET Core app** – serve extension webhooks locally or in production without extra wiring.
- 🔁 **Version-aware routing** – register multiple handlers per event and propagate version headers on outbound calls.
- 📦 **Manifest-aware defaults** – automatically loads `extension.yaml`/`manifest.yaml`, applies configuration defaults, and hydrates secrets from `KIKET_SECRET_*` environment variables.
- 📇 **Custom data client** – call `/api/v1/ext/custom_data/...` with `context.Endpoints.CustomData(projectId)` using the configured extension API key.
- 📉 **Rate-limit helper** – call `context.Endpoints.GetRateLimitAsync()` to inspect `/api/v1/ext/rate_limit` before launching heavy jobs.
- 🧱 **Typed & documented** – designed for .NET 8.0 with full type safety and rich XML documentation.
- 📊 **Telemetry & feedback hooks** – capture handler duration/success metrics automatically.

## Quickstart

```bash
dotnet add package Kiket.SDK
```

```csharp
using Kiket.SDK;

var sdk = new KiketSDK(new SDKConfig
{
    WebhookSecret = "sh_123",
    WorkspaceToken = "wk_test",
    ExtensionApiKey = Environment.GetEnvironmentVariable("KIKET_EXTENSION_API_KEY"),
    ExtensionId = "com.example.marketing",
    ExtensionVersion = "1.0.0"
});

// Register webhook handler (v1)
sdk.Register("issue.created", "v1", async (payload, context) =>
{
    var summary = payload["issue"]["title"].ToString();
    Console.WriteLine($"Event version: {context.EventVersion}");

    await context.Endpoints.LogEventAsync("issue.created", new Dictionary<string, object>
    {
        ["summary"] = summary
    });
    await context.Secrets.SetAsync("WEBHOOK_TOKEN", "abc123");

    return new { ok = true };
});

// Register webhook handler (v2)
sdk.Register("issue.created", "v2", async (payload, context) =>
{
    var summary = payload["issue"]["title"].ToString();

    await context.Endpoints.LogEventAsync("issue.created", new Dictionary<string, object>
    {
        ["summary"] = summary,
        ["schema"] = "v2"
    });

    return new { ok = true, version = context.EventVersion };
});

sdk.Run("0.0.0.0", 8080);
```

### Custom Data Client

When your manifest declares `custom_data.permissions`, set `ExtensionApiKey` (or the `KIKET_EXTENSION_API_KEY` environment variable) so outbound calls to the extension API include `X-Kiket-API-Key`. Use the helper to work with module data:

```csharp
sdk.Register("issue.created", "v1", async (payload, context) =>
{
    var projectId = payload["issue"]["project_id"].ToString();
    var customData = context.Endpoints.CustomData(projectId!);

    var list = await customData.ListAsync("com.example.crm.contacts", "automation_records", new CustomDataListOptions
    {
        Limit = 10,
        Filters = new Dictionary<string, object> { ["status"] = "active" }
    });

    await customData.CreateAsync("com.example.crm.contacts", "automation_records", new Dictionary<string, object>
    {
        ["email"] = "lead@example.com",
        ["metadata"] = new Dictionary<string, object> { ["source"] = "webhook" }
    });

    return new { synced = list?.Data.Count ?? 0 };
});
```

### SLA Alert Stream

Inspect the SLA alert feed for an installation:

```csharp
sdk.Register("workflow.sla_status", "v1", async (payload, context) =>
{
    var projectId = payload["issue"]["project_id"].ToString();
    var slaClient = context.Endpoints.SlaEvents(projectId!);

    var events = await slaClient.ListAsync(new SlaEventsListOptions
    {
        State = "imminent",
        Limit = 5
    });

    if (events?.Data?.Count == 0)
    {
        return new { ok = true };
    }

    var first = events!.Data![0];
    await context.Endpoints.LogEventAsync("sla.warning", new Dictionary<string, object>
    {
        ["issue_id"] = first["issue_id"],
        ["state"] = first["state"]
    });

    return new { acknowledged = true };
});
```

## Configuration

### Environment Variables

- `KIKET_WEBHOOK_SECRET` – Webhook HMAC secret for signature verification
- `KIKET_WORKSPACE_TOKEN` – Workspace token for API authentication
- `KIKET_EXTENSION_API_KEY` – Extension API key for `/api/v1/ext/**` endpoints (custom data client)
- `KIKET_BASE_URL` – Kiket API base URL (defaults to `https://kiket.dev`)
- `KIKET_SDK_TELEMETRY_URL` – Telemetry reporting endpoint (optional)
- `KIKET_SDK_TELEMETRY_OPTOUT` – Set to `1` to disable telemetry
- `KIKET_SECRET_*` – Secret overrides (e.g., `KIKET_SECRET_API_KEY`)

### Manifest File

Create an `extension.yaml` or `manifest.yaml` file:

```yaml
id: com.example.marketing
version: 1.0.0
delivery_secret: sh_production_secret

settings:
  - key: API_KEY
    secret: true
  - key: MAX_RETRIES
    default: 3
  - key: TIMEOUT_MS
    default: 5000
```

## API Reference

### KiketSDK

Main SDK class for building extensions.

```csharp
var sdk = new KiketSDK(new SDKConfig
{
    WebhookSecret = "...",
    WorkspaceToken = "...",
    BaseUrl = "...",
    Settings = new Dictionary<string, object>(),
    ExtensionId = "...",
    ExtensionVersion = "...",
    ManifestPath = "...",
    AutoEnvSecrets = true,
    TelemetryEnabled = true,
    FeedbackHook = record => { /* ... */ },
    TelemetryUrl = "..."
});
```

**Methods:**

- `sdk.Register(string event, string version, WebhookHandler handler)` – Register a webhook handler
- `sdk.Run(string host = "127.0.0.1", int port = 8000)` – Start the ASP.NET Core server
- `await sdk.StopAsync()` – Stop the server

### HandlerContext

Context passed to webhook handlers:

```csharp
public class HandlerContext
{
    public string Event { get; }
    public string EventVersion { get; }
    public Dictionary<string, string> Headers { get; }
    public KiketClient Client { get; }
    public ExtensionEndpoints Endpoints { get; }
    public Dictionary<string, object> Settings { get; }
    public string? ExtensionId { get; }
    public string? ExtensionVersion { get; }
    public ExtensionSecretManager Secrets { get; }
    public string? Secret(string key);  // Secret helper with payload-first fallback
}
```

### Secret Helper

The `Secret()` method provides a simple way to retrieve secrets with automatic fallback:

```csharp
// Checks payload secrets first (per-org config), falls back to ENV
var slackToken = context.Secret("SLACK_BOT_TOKEN");

// Example usage
sdk.Register("issue.created", "v1", async (payload, context) =>
{
    var apiKey = context.Secret("API_KEY");
    if (apiKey is null)
    {
        throw new InvalidOperationException("API_KEY not configured");
    }
    // Use apiKey...
    return new { ok = true };
});
```

The lookup order is:
1. **Payload secrets** (per-org configuration from `payload["secrets"]`)
2. **Environment variables** (extension defaults via `Environment.GetEnvironmentVariable()`)

This allows organizations to override extension defaults with their own credentials.

### ExtensionEndpoints

High-level extension endpoints:

```csharp
await context.Endpoints.LogEventAsync("event.name", data);
var metadata = await context.Endpoints.GetMetadataAsync();
```

### ExtensionSecretManager

Secret manager for CRUD operations:

```csharp
var value = await context.Secrets.GetAsync("API_KEY");
await context.Secrets.SetAsync("API_KEY", "new-value");
await context.Secrets.DeleteAsync("API_KEY");
var keys = await context.Secrets.ListAsync();
await context.Secrets.RotateAsync("API_KEY", "new-value");
```

## Publishing to GitHub Packages

When you are ready to cut a release:

1. Update the version in `Kiket.SDK.csproj`.
2. Run the test suite (`dotnet test`).
3. Build and pack:
   ```bash
   dotnet build --configuration Release
   dotnet pack --configuration Release
   ```
4. Commit and tag the release:
   ```bash
   git add Kiket.SDK/Kiket.SDK.csproj
   git commit -m "Bump .NET SDK to v0.x.y"
   git tag dotnet-v0.x.y
   git push --tags
   ```
5. GitHub Actions will automatically publish to GitHub Packages.

## License

MIT
### Rate-Limit Helper

Gate expensive automation against the current window:

```csharp
sdk.Register("automation.dispatch", "v1", async (_payload, context) =>
{
    var limits = await context.Endpoints.GetRateLimitAsync();
    if (limits is not null && limits.Remaining < 5)
    {
        await context.Endpoints.LogEventAsync("rate_limited", new()
        {
            ["remaining"] = limits.Remaining,
            ["reset_in"] = limits.ResetIn
        });
        return new { deferred = true };
    }

    // Continue with the heavy work
    return new { ok = true };
});
```
