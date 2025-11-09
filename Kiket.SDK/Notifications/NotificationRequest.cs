using System.Collections.Generic;

namespace Kiket.SDK.Notifications;

/// <summary>
/// Standard notification request for extension delivery.
/// </summary>
public class NotificationRequest
{
    /// <summary>
    /// The notification message content.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Type of channel ("channel", "dm", "group").
    /// </summary>
    public string ChannelType { get; set; } = "channel";

    /// <summary>
    /// ID of the channel (for ChannelType="channel").
    /// </summary>
    public string? ChannelId { get; set; }

    /// <summary>
    /// ID of the recipient (for ChannelType="dm").
    /// </summary>
    public string? RecipientId { get; set; }

    /// <summary>
    /// Message format ("plain", "markdown", "html").
    /// </summary>
    public string Format { get; set; } = "markdown";

    /// <summary>
    /// Notification priority ("low", "normal", "high", "urgent").
    /// </summary>
    public string Priority { get; set; } = "normal";

    /// <summary>
    /// Additional metadata for the notification.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Optional thread ID for threaded messages.
    /// </summary>
    public string? ThreadId { get; set; }

    /// <summary>
    /// Optional list of attachments.
    /// </summary>
    public List<Dictionary<string, object>>? Attachments { get; set; }

    /// <summary>
    /// Validates the notification request.
    /// </summary>
    /// <exception cref="System.ArgumentException">Thrown when validation fails.</exception>
    public void Validate()
    {
        if (string.IsNullOrEmpty(Message))
        {
            throw new System.ArgumentException("Message content is required");
        }

        var validChannelTypes = new[] { "channel", "dm", "group" };
        if (!System.Array.Exists(validChannelTypes, t => t == ChannelType))
        {
            throw new System.ArgumentException($"Invalid channelType: {ChannelType}");
        }

        if (ChannelType == "dm" && string.IsNullOrEmpty(RecipientId))
        {
            throw new System.ArgumentException("RecipientId is required for channelType='dm'");
        }

        if (ChannelType == "channel" && string.IsNullOrEmpty(ChannelId))
        {
            throw new System.ArgumentException("ChannelId is required for channelType='channel'");
        }

        var validFormats = new[] { "plain", "markdown", "html" };
        if (!System.Array.Exists(validFormats, f => f == Format))
        {
            throw new System.ArgumentException($"Invalid format: {Format}");
        }

        var validPriorities = new[] { "low", "normal", "high", "urgent" };
        if (!System.Array.Exists(validPriorities, p => p == Priority))
        {
            throw new System.ArgumentException($"Invalid priority: {Priority}");
        }
    }
}
