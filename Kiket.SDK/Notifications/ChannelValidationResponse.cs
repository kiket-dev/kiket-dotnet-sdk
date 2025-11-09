using System.Collections.Generic;

namespace Kiket.SDK.Notifications;

/// <summary>
/// Response from channel validation.
/// </summary>
public class ChannelValidationResponse
{
    /// <summary>
    /// Whether the channel is valid and accessible.
    /// </summary>
    public bool Valid { get; set; }

    /// <summary>
    /// Error message if validation failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Additional channel metadata (name, member count, etc.).
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Convert to dictionary for JSON serialization.
    /// </summary>
    public Dictionary<string, object> ToDictionary()
    {
        var result = new Dictionary<string, object>
        {
            ["valid"] = Valid
        };

        if (Error != null)
        {
            result["error"] = Error;
        }

        if (Metadata != null)
        {
            result["metadata"] = Metadata;
        }

        return result;
    }
}
