using System;
using System.Collections.Generic;

namespace Kiket.SDK.Notifications;

/// <summary>
/// Standard notification response from extension.
/// </summary>
public class NotificationResponse
{
    /// <summary>
    /// Whether the notification was delivered successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// ID of the delivered message.
    /// </summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// Timestamp when message was delivered.
    /// </summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>
    /// Error message if delivery failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Seconds to wait before retrying (for rate limits).
    /// </summary>
    public int? RetryAfter { get; set; }

    /// <summary>
    /// Convert to dictionary for JSON serialization.
    /// </summary>
    public Dictionary<string, object> ToDictionary()
    {
        var result = new Dictionary<string, object>
        {
            ["success"] = Success
        };

        if (MessageId != null)
        {
            result["message_id"] = MessageId;
        }

        if (DeliveredAt != null)
        {
            result["delivered_at"] = DeliveredAt.Value.ToString("o");
        }

        if (Error != null)
        {
            result["error"] = Error;
        }

        if (RetryAfter != null)
        {
            result["retry_after"] = RetryAfter.Value;
        }

        return result;
    }
}
