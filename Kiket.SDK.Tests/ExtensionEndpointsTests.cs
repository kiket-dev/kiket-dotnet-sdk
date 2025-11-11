using Kiket.SDK;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace Kiket.SDK.Tests;

public class ExtensionEndpointsTests
{
    [Fact]
    public async Task GetRateLimitAsync_ReturnsPayload()
    {
        var clientMock = new Mock<KiketClient>("https://api.example.com", "wk", "v1", null) { CallBase = true };
        clientMock
            .Setup(c => c.GetAsync<RateLimitResponse>("/api/v1/ext/rate_limit"))
            .ReturnsAsync(new RateLimitResponse
            {
                RateLimit = new RateLimitInfo
                {
                    Limit = 600,
                    Remaining = 42,
                    WindowSeconds = 60,
                    ResetIn = 12
                }
            });

        var endpoints = new ExtensionEndpoints(clientMock.Object, "ext-1", "v1");
        var info = await endpoints.GetRateLimitAsync();

        Assert.NotNull(info);
        Assert.Equal(600, info!.Limit);
        Assert.Equal(42, info.Remaining);
        Assert.Equal(60, info.WindowSeconds);
        Assert.Equal(12, info.ResetIn);

        clientMock.VerifyAll();
    }
}
