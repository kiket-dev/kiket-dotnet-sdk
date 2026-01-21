using System.Text.Json;
using Xunit;

namespace Kiket.SDK.Tests;

public class WebhookAuthTests
{
    public WebhookAuthTests()
    {
        WebhookAuth.ClearJwksCache();
    }

    [Fact]
    public void AuthenticationException_HasMessage()
    {
        var ex = new AuthenticationException("test error");
        Assert.Equal("test error", ex.Message);
    }

    [Fact]
    public void AuthenticationException_HasInnerException()
    {
        var inner = new Exception("inner");
        var ex = new AuthenticationException("test error", inner);
        Assert.Equal("test error", ex.Message);
        Assert.Equal(inner, ex.InnerException);
    }

    [Fact]
    public void IsAuthenticationError_ReturnsTrueForAuthException()
    {
        var authEx = new AuthenticationException("test");
        Assert.True(WebhookAuth.IsAuthenticationError(authEx));
    }

    [Fact]
    public void IsAuthenticationError_ReturnsFalseForOtherExceptions()
    {
        var regularEx = new Exception("not auth");
        Assert.False(WebhookAuth.IsAuthenticationError(regularEx));
    }

    [Fact]
    public void VerifyRuntimeToken_MissingAuthentication_Throws()
    {
        var payload = new Dictionary<string, object?>
        {
            ["event"] = "test"
        };

        var ex = Assert.Throws<AuthenticationException>(
            () => WebhookAuth.VerifyRuntimeToken(payload, "https://kiket.dev"));

        Assert.Equal("Missing runtime_token in payload", ex.Message);
    }

    [Fact]
    public void VerifyRuntimeToken_MissingToken_Throws()
    {
        var auth = JsonSerializer.SerializeToElement(new { });
        var payload = new Dictionary<string, object?>
        {
            ["authentication"] = auth
        };

        var ex = Assert.Throws<AuthenticationException>(
            () => WebhookAuth.VerifyRuntimeToken(payload, "https://kiket.dev"));

        Assert.Equal("Missing runtime_token in payload", ex.Message);
    }

    [Fact]
    public void VerifyRuntimeToken_EmptyToken_Throws()
    {
        var auth = JsonSerializer.SerializeToElement(new { runtime_token = "" });
        var payload = new Dictionary<string, object?>
        {
            ["authentication"] = auth
        };

        var ex = Assert.Throws<AuthenticationException>(
            () => WebhookAuth.VerifyRuntimeToken(payload, "https://kiket.dev"));

        Assert.Equal("Missing runtime_token in payload", ex.Message);
    }

    [Fact]
    public void BuildAuthContext_WithAllFields()
    {
        var exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        var jwtPayload = new WebhookAuth.JwtPayload(
            Sub: "test-subject",
            OrgId: 123,
            ExtId: 456,
            ProjId: 789,
            PiId: 111,
            Scopes: new List<string> { "read", "write" },
            Src: "webhook",
            Iss: "kiket.dev",
            Iat: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Exp: exp,
            Jti: "unique-id"
        );

        var auth = JsonSerializer.SerializeToElement(new { runtime_token = "test-token" });
        var rawPayload = new Dictionary<string, object?>
        {
            ["authentication"] = auth
        };

        var authCtx = WebhookAuth.BuildAuthContext(jwtPayload, rawPayload);

        Assert.Equal("test-token", authCtx.RuntimeToken);
        Assert.Equal("runtime", authCtx.TokenType);
        Assert.Equal(123, authCtx.OrgId);
        Assert.Equal(456, authCtx.ExtId);
        Assert.Equal(789, authCtx.ProjId);
        Assert.Equal(new List<string> { "read", "write" }, authCtx.Scopes);
        Assert.NotNull(authCtx.ExpiresAt);
    }

    [Fact]
    public void BuildAuthContext_NullScopes_ReturnsEmptyList()
    {
        var jwtPayload = new WebhookAuth.JwtPayload(
            Sub: "test-subject",
            OrgId: null,
            ExtId: null,
            ProjId: null,
            PiId: null,
            Scopes: null,
            Src: null,
            Iss: "kiket.dev",
            Iat: null,
            Exp: null,
            Jti: null
        );

        var rawPayload = new Dictionary<string, object?>();

        var authCtx = WebhookAuth.BuildAuthContext(jwtPayload, rawPayload);

        Assert.NotNull(authCtx.Scopes);
        Assert.Empty(authCtx.Scopes);
        Assert.Null(authCtx.ExpiresAt);
    }

    [Fact]
    public void BuildAuthContext_NoAuthentication_NullRuntimeToken()
    {
        var jwtPayload = new WebhookAuth.JwtPayload(
            Sub: "test-subject",
            OrgId: null,
            ExtId: null,
            ProjId: null,
            PiId: null,
            Scopes: null,
            Src: null,
            Iss: "kiket.dev",
            Iat: null,
            Exp: null,
            Jti: null
        );

        var rawPayload = new Dictionary<string, object?>();

        var authCtx = WebhookAuth.BuildAuthContext(jwtPayload, rawPayload);

        Assert.Null(authCtx.RuntimeToken);
        Assert.Equal("runtime", authCtx.TokenType);
    }

    [Fact]
    public void ClearJwksCache_DoesNotThrow()
    {
        var exception = Record.Exception(() => WebhookAuth.ClearJwksCache());
        Assert.Null(exception);
    }

    [Fact]
    public void JwtPayload_RecordProperties()
    {
        var payload = new WebhookAuth.JwtPayload(
            Sub: "sub",
            OrgId: 1,
            ExtId: 2,
            ProjId: 3,
            PiId: 4,
            Scopes: new List<string> { "scope1" },
            Src: "src",
            Iss: "iss",
            Iat: 100L,
            Exp: 200L,
            Jti: "jti"
        );

        Assert.Equal("sub", payload.Sub);
        Assert.Equal(1, payload.OrgId);
        Assert.Equal(2, payload.ExtId);
        Assert.Equal(3, payload.ProjId);
        Assert.Equal(4, payload.PiId);
        Assert.Single(payload.Scopes!);
        Assert.Equal("src", payload.Src);
        Assert.Equal("iss", payload.Iss);
        Assert.Equal(100L, payload.Iat);
        Assert.Equal(200L, payload.Exp);
        Assert.Equal("jti", payload.Jti);
    }

    [Fact]
    public void AuthContext_RecordProperties()
    {
        var now = DateTime.UtcNow;
        var authCtx = new WebhookAuth.AuthContext(
            RuntimeToken: "token",
            TokenType: "runtime",
            ExpiresAt: now,
            Scopes: new List<string> { "read" },
            OrgId: 1,
            ExtId: 2,
            ProjId: 3
        );

        Assert.Equal("token", authCtx.RuntimeToken);
        Assert.Equal("runtime", authCtx.TokenType);
        Assert.Equal(now, authCtx.ExpiresAt);
        Assert.Single(authCtx.Scopes);
        Assert.Equal(1, authCtx.OrgId);
        Assert.Equal(2, authCtx.ExtId);
        Assert.Equal(3, authCtx.ProjId);
    }
}
