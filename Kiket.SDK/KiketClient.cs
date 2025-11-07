using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Kiket.SDK;

/// <summary>
/// HTTP client for Kiket API.
/// </summary>
public class KiketClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string? _workspaceToken;
    private readonly string? _eventVersion;

    public KiketClient(string baseUrl, string? workspaceToken, string? eventVersion = null)
    {
        _workspaceToken = workspaceToken;
        _eventVersion = eventVersion;

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        };

        _httpClient.DefaultRequestHeaders.Add("User-Agent", "kiket-sdk-dotnet/0.1.0");
    }

    public virtual async Task<T?> GetAsync<T>(string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        AddAuthHeaders(request);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<T>();
    }

    public virtual async Task<T?> PostAsync<T>(string path, object data)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(data)
        };
        AddAuthHeaders(request);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<T>();
    }

    public virtual async Task<T?> PutAsync<T>(string path, object data)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, path)
        {
            Content = JsonContent.Create(data)
        };
        AddAuthHeaders(request);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<T>();
    }

    public virtual async Task<T?> DeleteAsync<T>(string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, path);
        AddAuthHeaders(request);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<T>();
    }

    private void AddAuthHeaders(HttpRequestMessage request)
    {
        if (!string.IsNullOrEmpty(_workspaceToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _workspaceToken);
        }

        if (!string.IsNullOrEmpty(_eventVersion))
        {
            request.Headers.Add("X-Kiket-Event-Version", _eventVersion);
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
