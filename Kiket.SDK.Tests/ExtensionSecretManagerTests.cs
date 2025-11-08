using Moq;
using Xunit;

namespace Kiket.SDK.Tests;

public class ExtensionSecretManagerTests
{
    private readonly Mock<KiketClient> _mockClient;
    private readonly ExtensionSecretManager _secretManager;

    public ExtensionSecretManagerTests()
    {
        _mockClient = new Mock<KiketClient>("https://api.test.com", "token", "v1", null);
        _secretManager = new ExtensionSecretManager(_mockClient.Object, "test-extension");
    }

    [Fact]
    public async Task GetAsync_ReturnsSecretValue()
    {
        _mockClient.Setup(c => c.GetAsync<Dictionary<string, string>>(
            "/extensions/test-extension/secrets/API_KEY"))
            .ReturnsAsync(new Dictionary<string, string> { ["value"] = "secret-value" });

        var result = await _secretManager.GetAsync("API_KEY");

        Assert.Equal("secret-value", result);
    }

    [Fact]
    public async Task GetAsync_ReturnsNullWhenNotFound()
    {
        _mockClient.Setup(c => c.GetAsync<Dictionary<string, string>>(
            "/extensions/test-extension/secrets/MISSING"))
            .ThrowsAsync(new Exception("404"));

        var result = await _secretManager.GetAsync("MISSING");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_ThrowsWhenNoExtensionId()
    {
        var manager = new ExtensionSecretManager(_mockClient.Object, null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => manager.GetAsync("API_KEY")
        );
    }

    [Fact]
    public async Task SetAsync_SetsSecretValue()
    {
        object? capturedPayload = null;
        _mockClient.Setup(c => c.PostAsync<object>(
            "/extensions/test-extension/secrets/API_KEY",
            It.IsAny<object>()))
            .ReturnsAsync(new object())
            .Callback<string, object>((_, payload) => capturedPayload = payload);

        await _secretManager.SetAsync("API_KEY", "new-value");

        _mockClient.Verify(c => c.PostAsync<object>(
            "/extensions/test-extension/secrets/API_KEY",
            It.IsAny<object>()),
            Times.Once);

        Assert.NotNull(capturedPayload);
    }

    [Fact]
    public async Task DeleteAsync_DeletesSecret()
    {
        _mockClient.Setup(c => c.DeleteAsync<object>(
            "/extensions/test-extension/secrets/API_KEY"))
            .ReturnsAsync(new object());

        await _secretManager.DeleteAsync("API_KEY");

        _mockClient.Verify(c => c.DeleteAsync<object>(
            "/extensions/test-extension/secrets/API_KEY"),
            Times.Once);
    }

    [Fact]
    public async Task ListAsync_ReturnsAllKeys()
    {
        _mockClient.Setup(c => c.GetAsync<Dictionary<string, List<string>>>(
            "/extensions/test-extension/secrets"))
            .ReturnsAsync(new Dictionary<string, List<string>>
            {
                ["keys"] = new List<string> { "API_KEY", "SECRET_TOKEN" }
            });

        var result = await _secretManager.ListAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains("API_KEY", result);
        Assert.Contains("SECRET_TOKEN", result);
    }

    [Fact]
    public async Task RotateAsync_DeletesAndSetsSecret()
    {
        _mockClient.Setup(c => c.DeleteAsync<object>(
            "/extensions/test-extension/secrets/API_KEY"))
            .ReturnsAsync(new object());
        _mockClient.Setup(c => c.PostAsync<object>(
            "/extensions/test-extension/secrets/API_KEY",
            It.IsAny<object>()))
            .ReturnsAsync(new object());

        await _secretManager.RotateAsync("API_KEY", "new-value");

        _mockClient.Verify(c => c.DeleteAsync<object>(
            "/extensions/test-extension/secrets/API_KEY"),
            Times.Once);
        _mockClient.Verify(c => c.PostAsync<object>(
            "/extensions/test-extension/secrets/API_KEY",
            It.IsAny<object>()),
            Times.Once);
    }
}
