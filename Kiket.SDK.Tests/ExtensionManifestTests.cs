using Xunit;

namespace Kiket.SDK.Tests;

public class ExtensionManifestTests
{
    [Fact]
    public void GetSettingsDefaults_ReturnsDefaultValues()
    {
        var manifest = new ExtensionManifest
        {
            Settings = new List<ManifestSetting>
            {
                new() { Key = "API_KEY", Secret = true },
                new() { Key = "MAX_RETRIES", Default = 3 },
                new() { Key = "TIMEOUT", Default = 5000 }
            }
        };

        var defaults = manifest.GetSettingsDefaults();

        Assert.Equal(2, defaults.Count);
        Assert.Equal(3, defaults["MAX_RETRIES"]);
        Assert.Equal(5000, defaults["TIMEOUT"]);
        Assert.False(defaults.ContainsKey("API_KEY"));
    }

    [Fact]
    public void GetSettingsDefaults_ReturnsEmptyWhenNoSettings()
    {
        var manifest = new ExtensionManifest();

        var defaults = manifest.GetSettingsDefaults();

        Assert.Empty(defaults);
    }

    [Fact]
    public void GetSecretKeys_ReturnsOnlySecretKeys()
    {
        var manifest = new ExtensionManifest
        {
            Settings = new List<ManifestSetting>
            {
                new() { Key = "API_KEY", Secret = true },
                new() { Key = "SECRET_TOKEN", Secret = true },
                new() { Key = "MAX_RETRIES", Default = 3 }
            }
        };

        var secretKeys = manifest.GetSecretKeys();

        Assert.Equal(2, secretKeys.Count);
        Assert.Contains("API_KEY", secretKeys);
        Assert.Contains("SECRET_TOKEN", secretKeys);
        Assert.DoesNotContain("MAX_RETRIES", secretKeys);
    }

    [Fact]
    public void GetSecretKeys_ReturnsEmptyWhenNoSecrets()
    {
        var manifest = new ExtensionManifest
        {
            Settings = new List<ManifestSetting>
            {
                new() { Key = "MAX_RETRIES", Default = 3 }
            }
        };

        var secretKeys = manifest.GetSecretKeys();

        Assert.Empty(secretKeys);
    }

    [Fact]
    public void ApplySecretEnvOverrides_AppliesEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable("KIKET_SECRET_API_KEY", "env-value");

        var manifest = new ExtensionManifest
        {
            Settings = new List<ManifestSetting>
            {
                new() { Key = "API_KEY", Secret = true }
            }
        };

        var overrides = manifest.ApplySecretEnvOverrides();

        Assert.Single(overrides);
        Assert.Equal("env-value", overrides["API_KEY"]);

        Environment.SetEnvironmentVariable("KIKET_SECRET_API_KEY", null);
    }

    [Fact]
    public void ApplySecretEnvOverrides_ReturnsEmptyWhenNoEnvVars()
    {
        var manifest = new ExtensionManifest
        {
            Settings = new List<ManifestSetting>
            {
                new() { Key = "API_KEY", Secret = true }
            }
        };

        var overrides = manifest.ApplySecretEnvOverrides();

        Assert.Empty(overrides);
    }
}
