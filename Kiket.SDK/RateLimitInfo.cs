namespace Kiket.SDK;

/// <summary>
/// Represents the current extension rate limit window.
/// </summary>
public record RateLimitInfo
{
    public int Limit { get; init; }
    public int Remaining { get; init; }
    public int WindowSeconds { get; init; }
    public int ResetIn { get; init; }
}

internal sealed class RateLimitResponse
{
    public RateLimitInfo? RateLimit { get; set; }
}
