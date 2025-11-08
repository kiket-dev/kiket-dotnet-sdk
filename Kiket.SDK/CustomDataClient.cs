using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kiket.SDK;

/// <summary>
/// Custom data client for extension-managed modules.
/// </summary>
public class CustomDataClient
{
    private readonly KiketClient _client;
    private readonly string _projectId;

    public CustomDataClient(KiketClient client, string projectId)
    {
        if (string.IsNullOrWhiteSpace(projectId))
        {
            throw new ArgumentException("projectId is required for custom data operations", nameof(projectId));
        }

        _client = client;
        _projectId = projectId;
    }

    public Task<CustomDataListResponse?> ListAsync(string moduleKey, string table, CustomDataListOptions? options = null)
    {
        var url = BuildUrl(moduleKey, table, null, options?.Limit, options?.Filters);
        return _client.GetAsync<CustomDataListResponse>(url);
    }

    public Task<CustomDataRecordResponse?> GetAsync(string moduleKey, string table, string recordId)
    {
        var url = BuildUrl(moduleKey, table, recordId);
        return _client.GetAsync<CustomDataRecordResponse>(url);
    }

    public Task<CustomDataRecordResponse?> CreateAsync(string moduleKey, string table, Dictionary<string, object> record)
    {
        var url = BuildUrl(moduleKey, table);
        return _client.PostAsync<CustomDataRecordResponse>(url, new { record });
    }

    public Task<CustomDataRecordResponse?> UpdateAsync(string moduleKey, string table, string recordId, Dictionary<string, object> record)
    {
        var url = BuildUrl(moduleKey, table, recordId);
        return _client.PatchAsync<CustomDataRecordResponse>(url, new { record });
    }

    public async Task DeleteAsync(string moduleKey, string table, string recordId)
    {
        var url = BuildUrl(moduleKey, table, recordId);
        await _client.DeleteAsync<object?>(url);
    }

    private static string Encode(string value) => Uri.EscapeDataString(value);

    private string BuildUrl(string moduleKey, string table, string? recordId = null, int? limit = null, Dictionary<string, object>? filters = null)
    {
        var path = $"/ext/custom_data/{Encode(moduleKey)}/{Encode(table)}";
        if (!string.IsNullOrEmpty(recordId))
        {
            path += $"/{recordId}";
        }

        var query = new List<string> { $"project_id={Encode(_projectId)}" };
        if (limit.HasValue)
        {
            query.Add($"limit={limit.Value}");
        }
        if (filters != null && filters.Count > 0)
        {
            var serialized = JsonSerializer.Serialize(filters);
            query.Add($"filters={Encode(serialized)}");
        }

        return $"{path}?{string.Join("&", query)}";
    }
}

public class CustomDataListOptions
{
    public int? Limit { get; set; }
    public Dictionary<string, object>? Filters { get; set; }
}

public class CustomDataListResponse
{
    [JsonPropertyName("data")]
    public List<Dictionary<string, object>> Data { get; set; } = new();
}

public class CustomDataRecordResponse
{
    [JsonPropertyName("data")]
    public Dictionary<string, object> Data { get; set; } = new();
}
