using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kiket.SDK;

/// <summary>
/// Client for managing intake forms and submissions via the Kiket API.
/// </summary>
public class IntakeFormsClient
{
    private readonly KiketClient _client;
    private readonly string _projectId;

    public IntakeFormsClient(KiketClient client, string projectId)
    {
        if (string.IsNullOrWhiteSpace(projectId))
        {
            throw new ArgumentException("projectId is required for intake form operations", nameof(projectId));
        }

        _client = client;
        _projectId = projectId;
    }

    /// <summary>
    /// List all intake forms for the project.
    /// </summary>
    public Task<IntakeFormListResponse?> ListAsync(IntakeFormListOptions? options = null)
    {
        var url = BuildUrl(null, null, options?.Active, options?.PublicOnly, options?.Limit, null, null);
        return _client.GetAsync<IntakeFormListResponse>(url);
    }

    /// <summary>
    /// Get a specific intake form by key or ID.
    /// </summary>
    public Task<IntakeForm?> GetAsync(string formKey)
    {
        if (string.IsNullOrWhiteSpace(formKey))
        {
            throw new ArgumentException("formKey is required", nameof(formKey));
        }

        var url = BuildUrl(formKey);
        return _client.GetAsync<IntakeForm>(url);
    }

    /// <summary>
    /// Get the public URL for a form if it's public.
    /// </summary>
    public string? PublicUrl(IntakeForm form)
    {
        if (form?.IsPublic == true)
        {
            return form.FormUrl;
        }
        return null;
    }

    /// <summary>
    /// List submissions for an intake form.
    /// </summary>
    public Task<IntakeSubmissionListResponse?> ListSubmissionsAsync(string formKey, IntakeSubmissionListOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(formKey))
        {
            throw new ArgumentException("formKey is required", nameof(formKey));
        }

        var url = BuildSubmissionsUrl(formKey, null, options?.Status, options?.Limit, options?.Since);
        return _client.GetAsync<IntakeSubmissionListResponse>(url);
    }

    /// <summary>
    /// Get a specific submission by ID.
    /// </summary>
    public Task<IntakeSubmission?> GetSubmissionAsync(string formKey, object submissionId)
    {
        if (string.IsNullOrWhiteSpace(formKey))
        {
            throw new ArgumentException("formKey is required", nameof(formKey));
        }
        if (submissionId == null)
        {
            throw new ArgumentException("submissionId is required", nameof(submissionId));
        }

        var url = BuildSubmissionsUrl(formKey, submissionId.ToString());
        return _client.GetAsync<IntakeSubmission>(url);
    }

    /// <summary>
    /// Create a new submission for an intake form.
    /// </summary>
    public Task<IntakeSubmission?> CreateSubmissionAsync(string formKey, Dictionary<string, object> data, Dictionary<string, object>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(formKey))
        {
            throw new ArgumentException("formKey is required", nameof(formKey));
        }
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data), "data is required");
        }

        var payload = new Dictionary<string, object>
        {
            ["project_id"] = _projectId,
            ["data"] = data
        };

        if (metadata != null)
        {
            payload["metadata"] = metadata;
        }

        var url = $"/ext/intake_forms/{Encode(formKey)}/submissions";
        return _client.PostAsync<IntakeSubmission>(url, payload);
    }

    /// <summary>
    /// Approve a pending submission.
    /// </summary>
    public Task<IntakeSubmission?> ApproveSubmissionAsync(string formKey, object submissionId, string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(formKey))
        {
            throw new ArgumentException("formKey is required", nameof(formKey));
        }
        if (submissionId == null)
        {
            throw new ArgumentException("submissionId is required", nameof(submissionId));
        }

        var payload = new Dictionary<string, object>
        {
            ["project_id"] = _projectId
        };

        if (!string.IsNullOrEmpty(notes))
        {
            payload["notes"] = notes;
        }

        var url = $"/ext/intake_forms/{Encode(formKey)}/submissions/{submissionId}/approve";
        return _client.PostAsync<IntakeSubmission>(url, payload);
    }

    /// <summary>
    /// Reject a pending submission.
    /// </summary>
    public Task<IntakeSubmission?> RejectSubmissionAsync(string formKey, object submissionId, string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(formKey))
        {
            throw new ArgumentException("formKey is required", nameof(formKey));
        }
        if (submissionId == null)
        {
            throw new ArgumentException("submissionId is required", nameof(submissionId));
        }

        var payload = new Dictionary<string, object>
        {
            ["project_id"] = _projectId
        };

        if (!string.IsNullOrEmpty(notes))
        {
            payload["notes"] = notes;
        }

        var url = $"/ext/intake_forms/{Encode(formKey)}/submissions/{submissionId}/reject";
        return _client.PostAsync<IntakeSubmission>(url, payload);
    }

    /// <summary>
    /// Get submission statistics for an intake form.
    /// </summary>
    public Task<IntakeFormStats?> StatsAsync(string formKey, string? period = null)
    {
        if (string.IsNullOrWhiteSpace(formKey))
        {
            throw new ArgumentException("formKey is required", nameof(formKey));
        }

        var query = new List<string> { $"project_id={Encode(_projectId)}" };
        if (!string.IsNullOrEmpty(period))
        {
            query.Add($"period={Encode(period)}");
        }

        var url = $"/ext/intake_forms/{Encode(formKey)}/stats?{string.Join("&", query)}";
        return _client.GetAsync<IntakeFormStats>(url);
    }

    private static string Encode(string value) => Uri.EscapeDataString(value);

    private string BuildUrl(
        string? formKey = null,
        string? submissionId = null,
        bool? active = null,
        bool? publicOnly = null,
        int? limit = null,
        string? status = null,
        string? since = null)
    {
        var path = "/ext/intake_forms";

        if (!string.IsNullOrEmpty(formKey))
        {
            path += $"/{Encode(formKey)}";
        }

        if (!string.IsNullOrEmpty(submissionId))
        {
            path += $"/submissions/{submissionId}";
        }

        var query = new List<string> { $"project_id={Encode(_projectId)}" };

        if (active.HasValue)
        {
            query.Add($"active={active.Value.ToString().ToLowerInvariant()}");
        }
        if (publicOnly.HasValue)
        {
            query.Add($"public={publicOnly.Value.ToString().ToLowerInvariant()}");
        }
        if (limit.HasValue)
        {
            query.Add($"limit={limit.Value}");
        }
        if (!string.IsNullOrEmpty(status))
        {
            query.Add($"status={Encode(status)}");
        }
        if (!string.IsNullOrEmpty(since))
        {
            query.Add($"since={Encode(since)}");
        }

        return $"{path}?{string.Join("&", query)}";
    }

    private string BuildSubmissionsUrl(
        string formKey,
        string? submissionId = null,
        string? status = null,
        int? limit = null,
        string? since = null)
    {
        var path = $"/ext/intake_forms/{Encode(formKey)}/submissions";

        if (!string.IsNullOrEmpty(submissionId))
        {
            path += $"/{submissionId}";
        }

        var query = new List<string> { $"project_id={Encode(_projectId)}" };

        if (!string.IsNullOrEmpty(status))
        {
            query.Add($"status={Encode(status)}");
        }
        if (limit.HasValue)
        {
            query.Add($"limit={limit.Value}");
        }
        if (!string.IsNullOrEmpty(since))
        {
            query.Add($"since={Encode(since)}");
        }

        return $"{path}?{string.Join("&", query)}";
    }
}

// Data classes

public class IntakeFormListOptions
{
    public bool? Active { get; set; }
    public bool? PublicOnly { get; set; }
    public int? Limit { get; set; }
}

public class IntakeSubmissionListOptions
{
    public string? Status { get; set; }
    public int? Limit { get; set; }
    public string? Since { get; set; }
}

public class IntakeFormListResponse
{
    [JsonPropertyName("data")]
    public List<IntakeForm> Data { get; set; } = new();
}

public class IntakeSubmissionListResponse
{
    [JsonPropertyName("data")]
    public List<IntakeSubmission> Data { get; set; } = new();
}

public class IntakeForm
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("active")]
    public bool Active { get; set; }

    [JsonPropertyName("public")]
    public bool IsPublic { get; set; }

    [JsonPropertyName("fields")]
    public List<IntakeFormField> Fields { get; set; } = new();

    [JsonPropertyName("form_url")]
    public string? FormUrl { get; set; }

    [JsonPropertyName("embed_allowed")]
    public bool EmbedAllowed { get; set; }

    [JsonPropertyName("submissions_count")]
    public int SubmissionsCount { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }
}

public class IntakeFormField
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("field_type")]
    public string FieldType { get; set; } = string.Empty;

    [JsonPropertyName("required")]
    public bool Required { get; set; }

    [JsonPropertyName("options")]
    public List<string>? Options { get; set; }

    [JsonPropertyName("placeholder")]
    public string? Placeholder { get; set; }

    [JsonPropertyName("help_text")]
    public string? HelpText { get; set; }
}

public class IntakeSubmission
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("intake_form_id")]
    public long IntakeFormId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public Dictionary<string, object> Data { get; set; } = new();

    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    [JsonPropertyName("submitted_by_email")]
    public string? SubmittedByEmail { get; set; }

    [JsonPropertyName("reviewed_by")]
    public string? ReviewedBy { get; set; }

    [JsonPropertyName("reviewed_at")]
    public string? ReviewedAt { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }
}

public class IntakeFormStats
{
    [JsonPropertyName("total_submissions")]
    public int TotalSubmissions { get; set; }

    [JsonPropertyName("pending")]
    public int Pending { get; set; }

    [JsonPropertyName("approved")]
    public int Approved { get; set; }

    [JsonPropertyName("rejected")]
    public int Rejected { get; set; }

    [JsonPropertyName("converted")]
    public int Converted { get; set; }

    [JsonPropertyName("period")]
    public string? Period { get; set; }
}
