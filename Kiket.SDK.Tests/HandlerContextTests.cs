using Xunit;

namespace Kiket.SDK.Tests;

public class HandlerContextTests
{
    [Fact]
    public void Secret_ReturnsPayloadValueWhenPresent()
    {
        var context = CreateContext(new Dictionary<string, string>
        {
            { "TEST_KEY", "payload-value" }
        });

        Assert.Equal("payload-value", context.Secret("TEST_KEY"));
    }

    [Fact]
    public void Secret_FallsBackToEnvWhenPayloadMissing()
    {
        Environment.SetEnvironmentVariable("HANDLER_TEST_ENV_KEY", "env-value");
        try
        {
            var context = CreateContext(new Dictionary<string, string>());

            Assert.Equal("env-value", context.Secret("HANDLER_TEST_ENV_KEY"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("HANDLER_TEST_ENV_KEY", null);
        }
    }

    [Fact]
    public void Secret_ReturnsNullWhenNotFound()
    {
        var context = CreateContext(new Dictionary<string, string>());

        Assert.Null(context.Secret("NONEXISTENT_KEY_12345"));
    }

    [Fact]
    public void Secret_PayloadTakesPriorityOverEnv()
    {
        Environment.SetEnvironmentVariable("PRIORITY_TEST_KEY", "env-value");
        try
        {
            var context = CreateContext(new Dictionary<string, string>
            {
                { "PRIORITY_TEST_KEY", "payload-value" }
            });

            Assert.Equal("payload-value", context.Secret("PRIORITY_TEST_KEY"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("PRIORITY_TEST_KEY", null);
        }
    }

    [Fact]
    public void Secret_IgnoresEmptyPayloadValue()
    {
        Environment.SetEnvironmentVariable("EMPTY_TEST_KEY", "env-value");
        try
        {
            var context = CreateContext(new Dictionary<string, string>
            {
                { "EMPTY_TEST_KEY", "" }
            });

            Assert.Equal("env-value", context.Secret("EMPTY_TEST_KEY"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("EMPTY_TEST_KEY", null);
        }
    }

    [Fact]
    public void Secret_HandlesEmptyPayloadSecretsDictionary()
    {
        Environment.SetEnvironmentVariable("EMPTY_DICT_TEST_KEY", "env-value");
        try
        {
            var context = CreateContext(new Dictionary<string, string>());

            Assert.Equal("env-value", context.Secret("EMPTY_DICT_TEST_KEY"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("EMPTY_DICT_TEST_KEY", null);
        }
    }

    private static HandlerContext CreateContext(Dictionary<string, string> payloadSecrets)
    {
        return new HandlerContext
        {
            Event = "test",
            EventVersion = "v1",
            Headers = new Dictionary<string, string>(),
            Client = null!,
            Endpoints = null!,
            Settings = new Dictionary<string, object>(),
            Secrets = null!,
            PayloadSecrets = payloadSecrets
        };
    }
}
