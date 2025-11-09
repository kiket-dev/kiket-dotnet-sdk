namespace Kiket.SDK;

/// <summary>
/// Helper for querying workflow SLA events.
/// </summary>
public class SlaEventsClient
{
    private readonly KiketClient _client;
    private readonly string _projectId;

    public SlaEventsClient(KiketClient client, string projectId)
    {
        if (string.IsNullOrWhiteSpace(projectId))
        {
            throw new ArgumentException("projectId is required", nameof(projectId));
        }

        _client = client;
        _projectId = projectId;
    }

    public Task<SlaEventsResponse?> ListAsync(SlaEventsListOptions? options = null)
    {
        var query = new List<string>
        {
            $"project_id={Uri.EscapeDataString(_projectId)}"
        };

        if (!string.IsNullOrWhiteSpace(options?.IssueId))
        {
            query.Add($"issue_id={Uri.EscapeDataString(options.IssueId!)}");
        }

        if (!string.IsNullOrWhiteSpace(options?.State))
        {
            query.Add($"state={Uri.EscapeDataString(options.State!)}");
        }

        if (options?.Limit != null)
        {
            query.Add($"limit={options.Limit.Value}");
        }

        var path = "/ext/sla/events";
        if (query.Count > 0)
        {
            path += "?" + string.Join("&", query);
        }

        return _client.GetAsync<SlaEventsResponse>(path);
    }
}

public class SlaEventsListOptions
{
    public string? IssueId { get; set; }
    public string? State { get; set; }
    public int? Limit { get; set; }
}

public class SlaEventsResponse
{
    public List<Dictionary<string, object>>? Data { get; set; }
}
