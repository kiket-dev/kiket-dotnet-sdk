using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kiket.SDK.Tests;

public class CustomDataClientTests
{
    [Fact]
    public async Task ListAsync_IncludesProjectParams()
    {
        var clientMock = new Mock<KiketClient>("https://api.example.com", null!, null!, null!) { CallBase = true };
        clientMock
            .Setup(c => c.GetAsync<CustomDataListResponse>("/ext/custom_data/com.example.module/records?project_id=42&limit=10&filters=%7B%22status%22%3A%22active%22%7D"))
            .ReturnsAsync(new CustomDataListResponse());

        var customData = new CustomDataClient(clientMock.Object, "42");
        await customData.ListAsync("com.example.module", "records", new CustomDataListOptions
        {
            Limit = 10,
            Filters = new Dictionary<string, object> { { "status", "active" } }
        });

        clientMock.VerifyAll();
    }

    [Fact]
    public async Task CreateAsync_PassesRecordBody()
    {
        var clientMock = new Mock<KiketClient>("https://api.example.com", null!, null!, null!) { CallBase = true };
        clientMock
            .Setup(c => c.PostAsync<CustomDataRecordResponse>(
                "/ext/custom_data/com.example.module/records?project_id=proj-1",
                It.Is<object>(o => RecordHasValue(o, "email", "lead@example.com"))))
            .ReturnsAsync(new CustomDataRecordResponse());

        var customData = new CustomDataClient(clientMock.Object, "proj-1");
        await customData.CreateAsync("com.example.module", "records", new Dictionary<string, object>
        {
            { "email", "lead@example.com" }
        });

        clientMock.VerifyAll();
    }

    [Fact]
    public async Task UpdateAsync_UsesPatchWithRecordBody()
    {
        var clientMock = new Mock<KiketClient>("https://api.example.com", null!, null!, null!) { CallBase = true };
        clientMock
            .Setup(c => c.PatchAsync<CustomDataRecordResponse>(
                "/ext/custom_data/com.example.module/records/7?project_id=proj-1",
                It.Is<object>(o => RecordHasValue(o, "status", "active"))))
            .ReturnsAsync(new CustomDataRecordResponse());

        var customData = new CustomDataClient(clientMock.Object, "proj-1");
        await customData.UpdateAsync(
            "com.example.module",
            "records",
            "7",
            new Dictionary<string, object> { { "status", "active" } });

        clientMock.VerifyAll();
    }

    [Fact]
    public async Task DeleteAsync_CallsDeleteEndpoint()
    {
        var clientMock = new Mock<KiketClient>("https://api.example.com", null!, null!, null!) { CallBase = true };
        clientMock
            .Setup(c => c.DeleteAsync<object?>("/ext/custom_data/com.example.module/records/9?project_id=42"))
            .ReturnsAsync((object?)null);

        var customData = new CustomDataClient(clientMock.Object, "42");
        await customData.DeleteAsync("com.example.module", "records", "9");

        clientMock.VerifyAll();
    }

    private static bool RecordHasValue(object payload, string key, string expected)
    {
        var property = payload.GetType().GetProperty("record");
        if (property?.GetValue(payload) is Dictionary<string, object> record &&
            record.TryGetValue(key, out var value) &&
            string.Equals(value?.ToString(), expected, StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }
}
