using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Kiket.SDK;

/// <summary>
/// Extension manifest.
/// </summary>
public class ExtensionManifest
{
    public string? Id { get; set; }
    public string? Version { get; set; }
    public string? DeliverySecret { get; set; }
    public List<ManifestSetting>? Settings { get; set; }

    public Dictionary<string, object> GetSettingsDefaults()
    {
        if (Settings == null) return new Dictionary<string, object>();

        return Settings
            .Where(s => s.Default != null)
            .ToDictionary(s => s.Key, s => s.Default!);
    }

    public List<string> GetSecretKeys()
    {
        if (Settings == null) return new List<string>();

        return Settings
            .Where(s => s.Secret)
            .Select(s => s.Key)
            .ToList();
    }

    public Dictionary<string, object> ApplySecretEnvOverrides()
    {
        var overrides = new Dictionary<string, object>();
        foreach (var key in GetSecretKeys())
        {
            var envKey = $"KIKET_SECRET_{key.ToUpperInvariant()}";
            var envValue = Environment.GetEnvironmentVariable(envKey);
            if (!string.IsNullOrEmpty(envValue))
            {
                overrides[key] = envValue;
            }
        }
        return overrides;
    }
}

/// <summary>
/// Manifest setting.
/// </summary>
public class ManifestSetting
{
    public required string Key { get; set; }
    public object? Default { get; set; }
    public bool Secret { get; set; }
}

/// <summary>
/// Loader for extension manifest files.
/// </summary>
public static class ManifestLoader
{
    public static ExtensionManifest? Load(string? manifestPath = null)
    {
        var paths = manifestPath != null
            ? new[] { manifestPath }
            : new[] { "extension.yaml", "manifest.yaml", "extension.yml", "manifest.yml" };

        foreach (var path in paths)
        {
            var fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
            {
                try
                {
                    var yaml = File.ReadAllText(fullPath);
                    var deserializer = new DeserializerBuilder()
                        .WithNamingConvention(UnderscoredNamingConvention.Instance)
                        .Build();

                    return deserializer.Deserialize<ExtensionManifest>(yaml);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to parse manifest at {fullPath}: {ex.Message}");
                }
            }
        }

        return null;
    }
}
