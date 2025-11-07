using Microsoft.AspNetCore.Http;
using Xunit;

namespace Kiket.SDK.Tests;

public class WebhookAuthTests
{
    private const string Secret = "test-secret";

    [Fact]
    public void VerifySignature_ValidSignature_DoesNotThrow()
    {
        var body = "{\"test\":\"data\"}";
        var (signature, timestamp) = WebhookAuth.GenerateSignature(Secret, body);

        var headers = new HeaderDictionary
        {
            ["X-Kiket-Signature"] = signature,
            ["X-Kiket-Timestamp"] = timestamp
        };

        var exception = Record.Exception(() => WebhookAuth.VerifySignature(Secret, body, headers));

        Assert.Null(exception);
    }

    [Fact]
    public void VerifySignature_NullSecret_ThrowsAuthenticationException()
    {
        var body = "{\"test\":\"data\"}";
        var headers = new HeaderDictionary();

        var exception = Assert.Throws<AuthenticationException>(
            () => WebhookAuth.VerifySignature(null, body, headers)
        );

        Assert.Contains("not configured", exception.Message);
    }

    [Fact]
    public void VerifySignature_MissingSignatureHeader_ThrowsAuthenticationException()
    {
        var body = "{\"test\":\"data\"}";
        var headers = new HeaderDictionary
        {
            ["X-Kiket-Timestamp"] = "123456789"
        };

        var exception = Assert.Throws<AuthenticationException>(
            () => WebhookAuth.VerifySignature(Secret, body, headers)
        );

        Assert.Contains("Missing X-Kiket-Signature", exception.Message);
    }

    [Fact]
    public void VerifySignature_MissingTimestampHeader_ThrowsAuthenticationException()
    {
        var body = "{\"test\":\"data\"}";
        var headers = new HeaderDictionary
        {
            ["X-Kiket-Signature"] = "abc123"
        };

        var exception = Assert.Throws<AuthenticationException>(
            () => WebhookAuth.VerifySignature(Secret, body, headers)
        );

        Assert.Contains("Missing X-Kiket-Timestamp", exception.Message);
    }

    [Fact]
    public void VerifySignature_InvalidSignature_ThrowsAuthenticationException()
    {
        var body = "{\"test\":\"data\"}";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var headers = new HeaderDictionary
        {
            ["X-Kiket-Signature"] = "invalid-signature",
            ["X-Kiket-Timestamp"] = timestamp
        };

        var exception = Assert.Throws<AuthenticationException>(
            () => WebhookAuth.VerifySignature(Secret, body, headers)
        );

        Assert.Contains("Invalid signature", exception.Message);
    }

    [Fact]
    public void VerifySignature_ExpiredTimestamp_ThrowsAuthenticationException()
    {
        var body = "{\"test\":\"data\"}";
        var oldTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 400; // 400 seconds ago
        var (signature, _) = WebhookAuth.GenerateSignature(Secret, body, oldTimestamp);

        var headers = new HeaderDictionary
        {
            ["X-Kiket-Signature"] = signature,
            ["X-Kiket-Timestamp"] = oldTimestamp.ToString()
        };

        var exception = Assert.Throws<AuthenticationException>(
            () => WebhookAuth.VerifySignature(Secret, body, headers)
        );

        Assert.Contains("too old", exception.Message);
    }

    [Fact]
    public void GenerateSignature_CreatesValidSignature()
    {
        var body = "{\"test\":\"data\"}";

        var (signature, timestamp) = WebhookAuth.GenerateSignature(Secret, body);

        Assert.NotEmpty(signature);
        Assert.Equal(64, signature.Length); // SHA-256 hex
        Assert.NotEmpty(timestamp);
    }

    [Fact]
    public void GenerateSignature_UsesProvidedTimestamp()
    {
        var body = "{\"test\":\"data\"}";
        var providedTimestamp = 1234567890L;

        var (_, timestamp) = WebhookAuth.GenerateSignature(Secret, body, providedTimestamp);

        Assert.Equal(providedTimestamp.ToString(), timestamp);
    }
}
