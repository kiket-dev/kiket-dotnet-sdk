namespace Kiket.SDK.Notifications;

/// <summary>
/// Request to validate a notification channel.
/// </summary>
public class ChannelValidationRequest
{
    /// <summary>
    /// ID of the channel to validate.
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Type of channel ("channel", "dm", "group").
    /// </summary>
    public string ChannelType { get; set; } = "channel";
}
