using Moq;
using Xunit;

namespace Kiket.SDK.Tests;

public class SlaEventsClientTests
{
    [Fact]
    public async Task ListAsyncBuildsQuery()
    {
        var client = new Mock<KiketClient>("https://example.com", "wk_test", null, null);
        client.Setup(c => c.GetAsync<SlaEventsResponse>(It.IsAny<string>()))
            .ReturnsAsync(new SlaEventsResponse());

        var sla = new SlaEventsClient(client.Object, "proj-1");
        await sla.ListAsync(new SlaEventsListOptions
        {
            IssueId = "77",
            State = "breached",
            Limit = 5
        });

        client.Verify(c => c.GetAsync<SlaEventsResponse>("/ext/sla/events?project_id=proj-1&issue_id=77&state=breached&limit=5"), Times.Once);
    }
}
