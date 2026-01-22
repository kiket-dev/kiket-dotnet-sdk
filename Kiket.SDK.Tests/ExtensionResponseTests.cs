using Kiket.SDK.Responses;
using Xunit;

namespace Kiket.SDK.Tests;

public class ExtensionResponseTests
{
    // Allow response tests

    [Fact]
    public void Allow_ReturnsProperlyFormattedResponse()
    {
        var response = ExtensionResponse.Allow().Build();

        Assert.Equal("allow", response.Status);
        Assert.Null(response.Message);
        Assert.Empty(response.Metadata);
    }

    [Fact]
    public void Allow_IncludesMessageWhenProvided()
    {
        var response = ExtensionResponse.Allow()
            .WithMessage("Success")
            .Build();

        Assert.Equal("Success", response.Message);
    }

    [Fact]
    public void Allow_IncludesDataInMetadata()
    {
        var response = ExtensionResponse.Allow()
            .WithData("routeId", 123)
            .WithData("email", "test@example.com")
            .Build();

        Assert.Equal(123, response.Metadata["routeId"]);
        Assert.Equal("test@example.com", response.Metadata["email"]);
    }

    [Fact]
    public void Allow_IncludesDataDictionaryInMetadata()
    {
        var data = new Dictionary<string, object?>
        {
            { "routeId", 456 },
            { "active", true }
        };

        var response = ExtensionResponse.Allow()
            .WithData(data)
            .Build();

        Assert.Equal(456, response.Metadata["routeId"]);
        Assert.Equal(true, response.Metadata["active"]);
    }

    [Fact]
    public void Allow_IncludesOutputFieldsInMetadata()
    {
        var response = ExtensionResponse.Allow()
            .WithOutputField("inbound_email", "abc@parse.example.com")
            .Build();

        var outputFields = (IReadOnlyDictionary<string, string>)response.Metadata["output_fields"]!;
        Assert.Equal("abc@parse.example.com", outputFields["inbound_email"]);
    }

    [Fact]
    public void Allow_IncludesOutputFieldsDictionaryInMetadata()
    {
        var fields = new Dictionary<string, string>
        {
            { "webhook_url", "https://example.com/hook" },
            { "api_key", "sk-xxx" }
        };

        var response = ExtensionResponse.Allow()
            .WithOutputFields(fields)
            .Build();

        var outputFields = (IReadOnlyDictionary<string, string>)response.Metadata["output_fields"]!;
        Assert.Equal("https://example.com/hook", outputFields["webhook_url"]);
        Assert.Equal("sk-xxx", outputFields["api_key"]);
    }

    [Fact]
    public void Allow_CombinesDataAndOutputFieldsInMetadata()
    {
        var response = ExtensionResponse.Allow()
            .WithMessage("Configured successfully")
            .WithData("routeId", 456)
            .WithOutputField("webhook_url", "https://example.com/hook")
            .Build();

        Assert.Equal("allow", response.Status);
        Assert.Equal("Configured successfully", response.Message);
        Assert.Equal(456, response.Metadata["routeId"]);

        var outputFields = (IReadOnlyDictionary<string, string>)response.Metadata["output_fields"]!;
        Assert.Equal("https://example.com/hook", outputFields["webhook_url"]);
    }

    // Deny response tests

    [Fact]
    public void Deny_ReturnsProperlyFormattedResponse()
    {
        var response = ExtensionResponse.Deny("Access denied").Build();

        Assert.Equal("deny", response.Status);
        Assert.Equal("Access denied", response.Message);
        Assert.Empty(response.Metadata);
    }

    [Fact]
    public void Deny_IncludesDataInMetadata()
    {
        var response = ExtensionResponse.Deny("Invalid credentials")
            .WithData("errorCode", "AUTH_FAILED")
            .Build();

        Assert.Equal("AUTH_FAILED", response.Metadata["errorCode"]);
    }

    [Fact]
    public void Deny_RequiresMessage()
    {
        Assert.Throws<ArgumentException>(() => ExtensionResponse.Deny(null!));
        Assert.Throws<ArgumentException>(() => ExtensionResponse.Deny(""));
    }

    // Pending response tests

    [Fact]
    public void Pending_ReturnsProperlyFormattedResponse()
    {
        var response = ExtensionResponse.Pending("Awaiting approval").Build();

        Assert.Equal("pending", response.Status);
        Assert.Equal("Awaiting approval", response.Message);
        Assert.Empty(response.Metadata);
    }

    [Fact]
    public void Pending_IncludesDataInMetadata()
    {
        var response = ExtensionResponse.Pending("Processing")
            .WithData("jobId", "abc123")
            .Build();

        Assert.Equal("abc123", response.Metadata["jobId"]);
    }

    [Fact]
    public void Pending_RequiresMessage()
    {
        Assert.Throws<ArgumentException>(() => ExtensionResponse.Pending(null!));
        Assert.Throws<ArgumentException>(() => ExtensionResponse.Pending(""));
    }

    // ToDictionary tests

    [Fact]
    public void ToDictionary_IncludesAllFieldsForAllow()
    {
        var response = ExtensionResponse.Allow()
            .WithMessage("Success")
            .WithData("key", "value")
            .WithOutputField("field", "output")
            .Build();

        var dict = response.ToDictionary();

        Assert.Equal("allow", dict["status"]);
        Assert.Equal("Success", dict["message"]);

        var metadata = (IReadOnlyDictionary<string, object?>)dict["metadata"]!;
        Assert.Equal("value", metadata["key"]);

        var outputFields = (IReadOnlyDictionary<string, string>)metadata["output_fields"]!;
        Assert.Equal("output", outputFields["field"]);
    }

    [Fact]
    public void ToDictionary_OmitsMessageWhenNull()
    {
        var response = ExtensionResponse.Allow().Build();

        var dict = response.ToDictionary();

        Assert.False(dict.ContainsKey("message"));
    }

    [Fact]
    public void ToDictionary_IncludesAllFieldsForDeny()
    {
        var response = ExtensionResponse.Deny("Error occurred")
            .WithData("details", "some error")
            .Build();

        var dict = response.ToDictionary();

        Assert.Equal("deny", dict["status"]);
        Assert.Equal("Error occurred", dict["message"]);

        var metadata = (IReadOnlyDictionary<string, object?>)dict["metadata"]!;
        Assert.Equal("some error", metadata["details"]);
    }

    // Immutability tests

    [Fact]
    public void Allow_HandlesNullDataDictionary()
    {
        var response = ExtensionResponse.Allow()
            .WithData((IDictionary<string, object?>)null!)
            .Build();

        Assert.Empty(response.Metadata);
    }

    [Fact]
    public void Allow_HandlesNullOutputFieldsDictionary()
    {
        var response = ExtensionResponse.Allow()
            .WithOutputFields(null!)
            .Build();

        Assert.False(response.Metadata.ContainsKey("output_fields"));
    }
}
