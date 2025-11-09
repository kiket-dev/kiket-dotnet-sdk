using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

namespace Kiket.SDK;

/// <summary>
/// Main SDK class for building Kiket extensions.
/// </summary>
public class KiketSDK
{
    private readonly SDKConfig _config;
    private readonly HandlerRegistry _registry;
    private readonly TelemetryReporter _telemetry;
    private readonly ExtensionManifest? _manifest;
    private WebApplication? _app;

    public KiketSDK(SDKConfig config)
    {
        _manifest = ManifestLoader.Load(config.ManifestPath);
        _config = ResolveConfig(config, _manifest);
        _registry = new HandlerRegistry();
        _telemetry = new TelemetryReporter(
            _config.TelemetryEnabled,
            _config.TelemetryUrl,
            _config.FeedbackHook,
            _config.ExtensionId,
            _config.ExtensionVersion,
            _config.ExtensionApiKey
        );
    }

    /// <summary>
    /// Register a webhook handler.
    /// </summary>
    public void Register(string eventName, string version, WebhookHandler handler)
    {
        _registry.Register(eventName, version, handler);
    }

    /// <summary>
    /// Start the ASP.NET Core web server.
    /// </summary>
    public void Run(string host = "127.0.0.1", int port = 8000)
    {
        var builder = WebApplication.CreateBuilder();

        // Configure URLs
        builder.WebHost.UseUrls($"http://{host}:{port}");

        _app = builder.Build();

        // Webhook endpoints
        _app.MapPost("/webhooks/{event}", async (string @event, HttpContext context) =>
        {
            return await DispatchWebhook(context, @event, null);
        });

        _app.MapPost("/v/{version}/webhooks/{event}", async (string version, string @event, HttpContext context) =>
        {
            return await DispatchWebhook(context, @event, version);
        });

        // Health check
        _app.MapGet("/health", () =>
        {
            return Results.Ok(new
            {
                status = "ok",
                extension_id = _config.ExtensionId,
                extension_version = _config.ExtensionVersion,
                registered_events = _registry.EventNames()
            });
        });

        Console.WriteLine($"🚀 Kiket extension listening on http://{host}:{port}");
        Console.WriteLine($"📦 Extension: {_config.ExtensionId ?? "unknown"}");
        Console.WriteLine($"📝 Registered events: {string.Join(", ", _registry.EventNames())}");

        _app.Run();
    }

    /// <summary>
    /// Stop the web server.
    /// </summary>
    public async Task StopAsync()
    {
        if (_app != null)
        {
            await _app.StopAsync();
        }
    }

    private async Task<IResult> DispatchWebhook(HttpContext context, string eventName, string? pathVersion)
    {
        // Read body
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();

        // Verify signature
        try
        {
            WebhookAuth.VerifySignature(_config.WebhookSecret, body, context.Request.Headers);
        }
        catch (AuthenticationException)
        {
            return Results.Unauthorized();
        }

        // Determine version
        var requestedVersion = pathVersion
            ?? context.Request.Headers["X-Kiket-Event-Version"].FirstOrDefault()
            ?? context.Request.Query["version"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(requestedVersion))
        {
            return Results.BadRequest(new { error = "Event version required" });
        }

        // Get handler
        var metadata = _registry.Get(eventName, requestedVersion);
        if (metadata == null)
        {
            return Results.NotFound(new { error = $"No handler registered for event '{eventName}' with version '{requestedVersion}'" });
        }

        // Parse payload
        var payload = JsonSerializer.Deserialize<Dictionary<string, object>>(body);

        // Create client and context
        var client = new KiketClient(_config.BaseUrl, _config.WorkspaceToken, metadata.Version, _config.ExtensionApiKey);
        var endpoints = new ExtensionEndpoints(client, _config.ExtensionId, metadata.Version);

        var handlerContext = new HandlerContext
        {
            Event = eventName,
            EventVersion = metadata.Version,
            Headers = context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()!),
            Client = client,
            Endpoints = endpoints,
            Settings = _config.Settings ?? new Dictionary<string, object>(),
            ExtensionId = _config.ExtensionId,
            ExtensionVersion = _config.ExtensionVersion,
            Secrets = endpoints.Secrets
        };

        // Execute handler with telemetry
        var startTime = DateTime.UtcNow;
        try
        {
            var result = await metadata.Handler(payload!, handlerContext);
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

            await _telemetry.RecordAsync(eventName, metadata.Version, "ok", duration);

            return Results.Ok(result ?? new { ok = true });
        }
        catch (Exception ex)
        {
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            await _telemetry.RecordAsync(eventName, metadata.Version, "error", duration, ex.Message, ex.GetType().Name);

            return Results.Problem(ex.Message);
        }
    }

    private SDKConfig ResolveConfig(SDKConfig config, ExtensionManifest? manifest)
    {
        var baseUrl = config.BaseUrl ?? Environment.GetEnvironmentVariable("KIKET_BASE_URL") ?? "https://kiket.dev";
        var workspaceToken = config.WorkspaceToken ?? Environment.GetEnvironmentVariable("KIKET_WORKSPACE_TOKEN");
        var webhookSecret = config.WebhookSecret
            ?? manifest?.DeliverySecret
            ?? Environment.GetEnvironmentVariable("KIKET_WEBHOOK_SECRET");
        var extensionApiKey = config.ExtensionApiKey ?? Environment.GetEnvironmentVariable("KIKET_EXTENSION_API_KEY");

        var settings = new Dictionary<string, object>();
        if (manifest != null)
        {
            foreach (var kv in manifest.GetSettingsDefaults())
            {
                settings[kv.Key] = kv.Value;
            }
            if (config.AutoEnvSecrets)
            {
                foreach (var kv in manifest.ApplySecretEnvOverrides())
                {
                    settings[kv.Key] = kv.Value;
                }
            }
        }
        if (config.Settings != null)
        {
            foreach (var kv in config.Settings)
            {
                settings[kv.Key] = kv.Value;
            }
        }

        var extensionId = config.ExtensionId ?? manifest?.Id;
        var extensionVersion = config.ExtensionVersion ?? manifest?.Version;
        var telemetryUrl = config.TelemetryUrl
            ?? Environment.GetEnvironmentVariable("KIKET_SDK_TELEMETRY_URL")
            ?? $"{baseUrl.TrimEnd('/')}/api/v1/ext";

        return new SDKConfig
        {
            WebhookSecret = webhookSecret,
            WorkspaceToken = workspaceToken,
            BaseUrl = baseUrl,
            Settings = settings,
            ExtensionId = extensionId,
            ExtensionVersion = extensionVersion,
            ManifestPath = config.ManifestPath,
            AutoEnvSecrets = config.AutoEnvSecrets,
            TelemetryEnabled = config.TelemetryEnabled,
            FeedbackHook = config.FeedbackHook,
            TelemetryUrl = telemetryUrl,
            ExtensionApiKey = extensionApiKey
        };
    }
}
