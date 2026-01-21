using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace Kiket.SDK;

/// <summary>
/// Authentication exception.
/// </summary>
public class AuthenticationException : Exception
{
    public AuthenticationException(string message) : base(message) { }
    public AuthenticationException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// JWT verification for webhook payloads.
/// Verifies runtime tokens are signed by Kiket using ES256 (ECDSA P-256).
/// </summary>
public static class WebhookAuth
{
    private const string Algorithm = "ES256";
    private const string Issuer = "kiket.dev";
    private static readonly TimeSpan JwksCacheTtl = TimeSpan.FromHours(1);
    private static readonly TimeSpan HttpTimeout = TimeSpan.FromSeconds(10);

    private static readonly ConcurrentDictionary<string, JwksCacheEntry> JwksCache = new();
    private static readonly HttpClient HttpClient = new() { Timeout = HttpTimeout };

    private record JwksCacheEntry(JsonWebKeySet Jwks, DateTime FetchedAt);

    /// <summary>
    /// Decoded JWT payload.
    /// </summary>
    public record JwtPayload(
        string? Sub,
        int? OrgId,
        int? ExtId,
        int? ProjId,
        int? PiId,
        List<string>? Scopes,
        string? Src,
        string? Iss,
        long? Iat,
        long? Exp,
        string? Jti
    );

    /// <summary>
    /// Authentication context from verified JWT.
    /// </summary>
    public record AuthContext(
        string? RuntimeToken,
        string TokenType,
        DateTime? ExpiresAt,
        List<string> Scopes,
        int? OrgId,
        int? ExtId,
        int? ProjId
    );

    /// <summary>
    /// Verify the runtime token JWT from the payload.
    /// </summary>
    public static JwtPayload VerifyRuntimeToken(Dictionary<string, object?> payload, string baseUrl)
    {
        if (!payload.TryGetValue("authentication", out var authObj) || authObj is not JsonElement authElement)
        {
            throw new AuthenticationException("Missing runtime_token in payload");
        }

        string? token = null;
        if (authElement.TryGetProperty("runtime_token", out var tokenElement))
        {
            token = tokenElement.GetString();
        }

        if (string.IsNullOrEmpty(token))
        {
            throw new AuthenticationException("Missing runtime_token in payload");
        }

        return DecodeJwt(token, baseUrl);
    }

    /// <summary>
    /// Decode and verify a JWT token using the public key from JWKS.
    /// </summary>
    public static JwtPayload DecodeJwt(string token, string baseUrl)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Verify algorithm
            if (jwtToken.Header.Alg != Algorithm)
            {
                throw new AuthenticationException($"Unexpected signing method: {jwtToken.Header.Alg}");
            }

            // Fetch JWKS and find signing key
            var jwks = FetchJwks(baseUrl);
            var kid = jwtToken.Header.Kid;
            var securityKey = FindSigningKey(jwks, kid);

            // Validate token
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = Issuer,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = securityKey,
                ClockSkew = TimeSpan.Zero
            };

            handler.ValidateToken(token, validationParameters, out _);

            return ParseJwtPayload(jwtToken);
        }
        catch (SecurityTokenExpiredException)
        {
            throw new AuthenticationException("Runtime token has expired");
        }
        catch (SecurityTokenInvalidIssuerException)
        {
            throw new AuthenticationException("Invalid token issuer");
        }
        catch (SecurityTokenException ex)
        {
            throw new AuthenticationException($"Invalid token: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Fetch JWKS from the well-known endpoint with caching.
    /// </summary>
    public static JsonWebKeySet FetchJwks(string baseUrl)
    {
        if (JwksCache.TryGetValue(baseUrl, out var cached) &&
            DateTime.UtcNow - cached.FetchedAt < JwksCacheTtl)
        {
            return cached.Jwks;
        }

        var jwksUrl = baseUrl.TrimEnd('/') + "/.well-known/jwks.json";

        try
        {
            var response = HttpClient.GetAsync(jwksUrl).GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
            {
                throw new AuthenticationException($"Failed to fetch JWKS: status {(int)response.StatusCode}");
            }

            var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var jwks = new JsonWebKeySet(content);

            JwksCache[baseUrl] = new JwksCacheEntry(jwks, DateTime.UtcNow);
            return jwks;
        }
        catch (HttpRequestException ex)
        {
            throw new AuthenticationException($"Failed to fetch JWKS: {ex.Message}", ex);
        }
        catch (JsonException)
        {
            throw new AuthenticationException("Invalid JWKS response");
        }
    }

    /// <summary>
    /// Clear the JWKS cache (useful for testing or key rotation).
    /// </summary>
    public static void ClearJwksCache()
    {
        JwksCache.Clear();
    }

    /// <summary>
    /// Build authentication context from verified JWT payload.
    /// </summary>
    public static AuthContext BuildAuthContext(JwtPayload jwtPayload, Dictionary<string, object?> rawPayload)
    {
        string? runtimeToken = null;
        if (rawPayload.TryGetValue("authentication", out var authObj) && authObj is JsonElement authElement)
        {
            if (authElement.TryGetProperty("runtime_token", out var tokenElement))
            {
                runtimeToken = tokenElement.GetString();
            }
        }

        DateTime? expiresAt = jwtPayload.Exp.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(jwtPayload.Exp.Value).UtcDateTime
            : null;

        var scopes = jwtPayload.Scopes ?? new List<string>();

        return new AuthContext(
            runtimeToken,
            "runtime",
            expiresAt,
            scopes,
            jwtPayload.OrgId,
            jwtPayload.ExtId,
            jwtPayload.ProjId
        );
    }

    /// <summary>
    /// Check if an exception is an authentication error.
    /// </summary>
    public static bool IsAuthenticationError(Exception ex)
    {
        return ex is AuthenticationException;
    }

    private static SecurityKey FindSigningKey(JsonWebKeySet jwks, string? kid)
    {
        foreach (var key in jwks.Keys)
        {
            if (key.Use != "sig")
            {
                continue;
            }
            if (key.Alg != Algorithm)
            {
                continue;
            }
            if (!string.IsNullOrEmpty(kid) && key.Kid != kid)
            {
                continue;
            }
            if (key.Kty == "EC")
            {
                return BuildEcdsaSecurityKey(key);
            }
        }

        throw new AuthenticationException("No suitable signing key found in JWKS");
    }

    private static SecurityKey BuildEcdsaSecurityKey(JsonWebKey jwk)
    {
        if (jwk.Crv != "P-256")
        {
            throw new AuthenticationException($"Unsupported curve: {jwk.Crv}");
        }

        var xBytes = Base64UrlEncoder.DecodeBytes(jwk.X);
        var yBytes = Base64UrlEncoder.DecodeBytes(jwk.Y);

        var ecParameters = new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint
            {
                X = xBytes,
                Y = yBytes
            }
        };

        var ecdsa = ECDsa.Create(ecParameters);
        return new ECDsaSecurityKey(ecdsa);
    }

    private static JwtPayload ParseJwtPayload(JwtSecurityToken token)
    {
        int? GetIntClaim(string name)
        {
            var claim = token.Claims.FirstOrDefault(c => c.Type == name);
            return claim != null && int.TryParse(claim.Value, out var val) ? val : null;
        }

        long? GetLongClaim(string name)
        {
            var claim = token.Claims.FirstOrDefault(c => c.Type == name);
            return claim != null && long.TryParse(claim.Value, out var val) ? val : null;
        }

        string? GetStringClaim(string name)
        {
            return token.Claims.FirstOrDefault(c => c.Type == name)?.Value;
        }

        var scopes = token.Claims
            .Where(c => c.Type == "scopes")
            .Select(c => c.Value)
            .ToList();

        return new JwtPayload(
            token.Subject,
            GetIntClaim("org_id"),
            GetIntClaim("ext_id"),
            GetIntClaim("proj_id"),
            GetIntClaim("pi_id"),
            scopes.Count > 0 ? scopes : null,
            GetStringClaim("src"),
            token.Issuer,
            GetLongClaim("iat"),
            GetLongClaim("exp"),
            token.Id
        );
    }
}
