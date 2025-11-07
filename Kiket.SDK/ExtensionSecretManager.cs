namespace Kiket.SDK;

/// <summary>
/// Extension secret manager.
/// </summary>
public class ExtensionSecretManager
{
    private readonly KiketClient _client;
    private readonly string? _extensionId;

    public ExtensionSecretManager(KiketClient client, string? extensionId)
    {
        _client = client;
        _extensionId = extensionId;
    }

    public async Task<string?> GetAsync(string key)
    {
        if (string.IsNullOrEmpty(_extensionId))
        {
            throw new InvalidOperationException("Extension ID required for secret operations");
        }

        try
        {
            var response = await _client.GetAsync<Dictionary<string, string>>(
                $"/extensions/{_extensionId}/secrets/{key}");
            return response?["value"];
        }
        catch
        {
            return null;
        }
    }

    public async Task SetAsync(string key, string value)
    {
        if (string.IsNullOrEmpty(_extensionId))
        {
            throw new InvalidOperationException("Extension ID required for secret operations");
        }

        await _client.PostAsync<object>(
            $"/extensions/{_extensionId}/secrets/{key}",
            new { value });
    }

    public async Task DeleteAsync(string key)
    {
        if (string.IsNullOrEmpty(_extensionId))
        {
            throw new InvalidOperationException("Extension ID required for secret operations");
        }

        await _client.DeleteAsync<object>($"/extensions/{_extensionId}/secrets/{key}");
    }

    public async Task<List<string>> ListAsync()
    {
        if (string.IsNullOrEmpty(_extensionId))
        {
            throw new InvalidOperationException("Extension ID required for secret operations");
        }

        var response = await _client.GetAsync<Dictionary<string, List<string>>>(
            $"/extensions/{_extensionId}/secrets");
        return response?["keys"] ?? new List<string>();
    }

    public async Task RotateAsync(string key, string newValue)
    {
        await DeleteAsync(key);
        await SetAsync(key, newValue);
    }
}
