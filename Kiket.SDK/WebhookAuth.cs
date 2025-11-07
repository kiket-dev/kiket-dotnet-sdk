using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Kiket.SDK;

/// <summary>
/// Authentication exception.
/// </summary>
public class AuthenticationException : Exception
{
    public AuthenticationException(string message) : base(message) { }
}

/// <summary>
/// HMAC signature verification for webhook payloads.
/// </summary>
public static class WebhookAuth
{
    public static void VerifySignature(string? secret, string body, IHeaderDictionary headers)
    {
        if (string.IsNullOrEmpty(secret))
        {
            throw new AuthenticationException("Webhook secret not configured");
        }

        var signature = headers["X-Kiket-Signature"].FirstOrDefault();
        var timestamp = headers["X-Kiket-Timestamp"].FirstOrDefault();

        if (string.IsNullOrEmpty(signature))
        {
            throw new AuthenticationException("Missing X-Kiket-Signature header");
        }

        if (string.IsNullOrEmpty(timestamp))
        {
            throw new AuthenticationException("Missing X-Kiket-Timestamp header");
        }

        // Check timestamp (5 minute window)
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (!long.TryParse(timestamp, out var requestTime))
        {
            throw new AuthenticationException("Invalid X-Kiket-Timestamp header");
        }

        var timeDiff = Math.Abs(now - requestTime);
        if (timeDiff > 300)
        {
            throw new AuthenticationException($"Request timestamp too old or too far in future: {timeDiff}s");
        }

        // Compute expected signature
        var payload = $"{timestamp}.{body}";
        var expectedSignature = ComputeHmacSha256(secret, payload);

        // Constant-time comparison
        if (!CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(signature),
            Encoding.UTF8.GetBytes(expectedSignature)))
        {
            throw new AuthenticationException("Invalid signature");
        }
    }

    public static (string Signature, string Timestamp) GenerateSignature(string secret, string body, long? timestamp = null)
    {
        var ts = timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var tsStr = ts.ToString();
        var payload = $"{tsStr}.{body}";
        var signature = ComputeHmacSha256(secret, payload);

        return (signature, tsStr);
    }

    private static string ComputeHmacSha256(string key, string data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
