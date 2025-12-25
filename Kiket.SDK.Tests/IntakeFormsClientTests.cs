using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kiket.SDK.Tests;

public class IntakeFormsClientTests
{
    [Fact]
    public void Constructor_ThrowsWhenProjectIdIsNull()
    {
        var clientMock = new Mock<KiketClient>("https://api.example.com", null!, null!, null!) { CallBase = true };
        Assert.Throws<ArgumentException>(() => new IntakeFormsClient(clientMock.Object, null!));
    }

    [Fact]
    public void Constructor_ThrowsWhenProjectIdIsEmpty()
    {
        var clientMock = new Mock<KiketClient>("https://api.example.com", null!, null!, null!) { CallBase = true };
        Assert.Throws<ArgumentException>(() => new IntakeFormsClient(clientMock.Object, "   "));
    }

    [Fact]
    public async Task ListAsync_IncludesProjectId()
    {
        var clientMock = new Mock<KiketClient>("https://api.example.com", null!, null!, null!) { CallBase = true };
        clientMock
            .Setup(c => c.GetAsync<IntakeFormListResponse>(It.Is<string>(url =>
                url.Contains("project_id=42") && url.Contains("/ext/intake_forms"))))
            .ReturnsAsync(new IntakeFormListResponse());

        var intakeForms = new IntakeFormsClient(clientMock.Object, "42");
        await intakeForms.ListAsync();

        clientMock.VerifyAll();
    }

    [Fact]
    public async Task ListAsync_IncludesOptionalFilters()
    {
        var clientMock = new Mock<KiketClient>("https://api.example.com", null!, null!, null!) { CallBase = true };
        clientMock
            .Setup(c => c.GetAsync<IntakeFormListResponse>(It.Is<string>(url =>
                url.Contains("active=true") &&
                url.Contains("public=true") &&
                url.Contains("limit=10"))))
            .ReturnsAsync(new IntakeFormListResponse());

        var intakeForms = new IntakeFormsClient(clientMock.Object, "42");
        await intakeForms.ListAsync(new IntakeFormListOptions
        {
            Active = true,
            PublicOnly = true,
            Limit = 10
        });

        clientMock.VerifyAll();
    }

    [Fact]
    public async Task GetAsync_ReturnsForm()
    {
        var clientMock = new Mock<KiketClient>("https://api.example.com", null!, null!, null!) { CallBase = true };
        clientMock
            .Setup(c => c.GetAsync<IntakeForm>(It.Is<string>(url =>
                url.Contains("/ext/intake_forms/feedback") && url.Contains("project_id=42"))))
            .ReturnsAsync(new IntakeForm { Key = "feedback", Name = "Feedback Form" });

        var intakeForms = new IntakeFormsClient(clientMock.Object, "42");
        var result = await intakeForms.GetAsync("feedback");

        Assert.NotNull(result);
        Assert.Equal("feedback", result!.Key);
        clientMock.VerifyAll();
    }

    [Fact]
    public async Task GetAsync_ThrowsWhenFormKeyIsEmpty()
    {
        var clientMock = new Mock<KiketClient>("https://api.example.com", null!, null!, null!) { CallBase = true };
        var intakeForms = new IntakeFormsClient(clientMock.Object, "42");

        await Assert.ThrowsAsync<ArgumentException>(() => intakeForms.GetAsync(""));
    }

    [Fact]
    public void PublicUrl_ReturnsUrlForPublicForm()
    {
        var clientMock = new Mock<KiketClient>("https://api.example.com", null!, null!, null!) { CallBase = true };
        var intakeForms = new IntakeFormsClient(clientMock.Object, "42");

        var form = new IntakeForm
        {
            Key = "feedback",
            IsPublic = true,
            FormUrl = "https://app.kiket.dev/forms/feedback"
        };

        Assert.Equal("https://app.kiket.dev/forms/feedback", intakeForms.PublicUrl(form));
    }

    [Fact]
    public void PublicUrl_ReturnsNullForPrivateForm()
    {
        var clientMock = new Mock<KiketClient>("https://api.example.com", null!, null!, null!) { CallBase = true };
        var intakeForms = new IntakeFormsClient(clientMock.Object, "42");

        var form = new IntakeForm
        {
            Key = "internal",
            IsPublic = false
        };

        Assert.Null(intakeForms.PublicUrl(form));
    }

    [Fact]
    public async Task ListSubmissionsAsync_IncludesFilters()
    {
        var clientMock = new Mock<KiketClient>("https://api.example.com", null!, null!, null!) { CallBase = true };
        clientMock
            .Setup(c => c.GetAsync<IntakeSubmissionListResponse>(It.Is<string>(url =>
                url.Contains("/submissions") &&
                url.Contains("status=pending") &&
                url.Contains("limit=25"))))
            .ReturnsAsync(new IntakeSubmissionListResponse());

        var intakeForms = new IntakeFormsClient(clientMock.Object, "42");
        await intakeForms.ListSubmissionsAsync("feedback", new IntakeSubmissionListOptions
        {
            Status = "pending",
            Limit = 25
        });

        clientMock.VerifyAll();
    }

    [Fact]
    public async Task CreateSubmissionAsync_SendsPayload()
    {
        var clientMock = new Mock<KiketClient>("https://api.example.com", null!, null!, null!) { CallBase = true };
        clientMock
            .Setup(c => c.PostAsync<IntakeSubmission>(
                It.Is<string>(url => url.Contains("/ext/intake_forms/feedback/submissions")),
                It.Is<object>(o => HasProjectIdAndData(o, "42"))))
            .ReturnsAsync(new IntakeSubmission { Status = "pending" });

        var intakeForms = new IntakeFormsClient(clientMock.Object, "42");
        var result = await intakeForms.CreateSubmissionAsync("feedback", new Dictionary<string, object>
        {
            { "email", "test@example.com" }
        });

        Assert.NotNull(result);
        Assert.Equal("pending", result!.Status);
        clientMock.VerifyAll();
    }

    [Fact]
    public async Task CreateSubmissionAsync_IncludesMetadata()
    {
        var clientMock = new Mock<KiketClient>("https://api.example.com", null!, null!, null!) { CallBase = true };
        clientMock
            .Setup(c => c.PostAsync<IntakeSubmission>(
                It.IsAny<string>(),
                It.Is<object>(o => HasMetadata(o))))
            .ReturnsAsync(new IntakeSubmission());

        var intakeForms = new IntakeFormsClient(clientMock.Object, "42");
        await intakeForms.CreateSubmissionAsync(
            "feedback",
            new Dictionary<string, object> { { "email", "test@example.com" } },
            new Dictionary<string, object> { { "source", "api" } });

        clientMock.VerifyAll();
    }

    [Fact]
    public async Task ApproveSubmissionAsync_SendsPayload()
    {
        var clientMock = new Mock<KiketClient>("https://api.example.com", null!, null!, null!) { CallBase = true };
        clientMock
            .Setup(c => c.PostAsync<IntakeSubmission>(
                It.Is<string>(url => url.Contains("/approve")),
                It.Is<object>(o => HasNotes(o, "Looks good!"))))
            .ReturnsAsync(new IntakeSubmission { Status = "approved" });

        var intakeForms = new IntakeFormsClient(clientMock.Object, "42");
        var result = await intakeForms.ApproveSubmissionAsync("feedback", 1, "Looks good!");

        Assert.NotNull(result);
        Assert.Equal("approved", result!.Status);
        clientMock.VerifyAll();
    }

    [Fact]
    public async Task RejectSubmissionAsync_SendsPayload()
    {
        var clientMock = new Mock<KiketClient>("https://api.example.com", null!, null!, null!) { CallBase = true };
        clientMock
            .Setup(c => c.PostAsync<IntakeSubmission>(
                It.Is<string>(url => url.Contains("/reject")),
                It.Is<object>(o => HasNotes(o, "Invalid data"))))
            .ReturnsAsync(new IntakeSubmission { Status = "rejected" });

        var intakeForms = new IntakeFormsClient(clientMock.Object, "42");
        var result = await intakeForms.RejectSubmissionAsync("feedback", 1, "Invalid data");

        Assert.NotNull(result);
        Assert.Equal("rejected", result!.Status);
        clientMock.VerifyAll();
    }

    [Fact]
    public async Task StatsAsync_ReturnsStatistics()
    {
        var clientMock = new Mock<KiketClient>("https://api.example.com", null!, null!, null!) { CallBase = true };
        clientMock
            .Setup(c => c.GetAsync<IntakeFormStats>(It.Is<string>(url =>
                url.Contains("/stats") && url.Contains("period=month"))))
            .ReturnsAsync(new IntakeFormStats
            {
                TotalSubmissions = 100,
                Pending = 10,
                Approved = 80
            });

        var intakeForms = new IntakeFormsClient(clientMock.Object, "42");
        var result = await intakeForms.StatsAsync("feedback", "month");

        Assert.NotNull(result);
        Assert.Equal(100, result!.TotalSubmissions);
        clientMock.VerifyAll();
    }

    private static bool HasProjectIdAndData(object payload, string projectId)
    {
        if (payload is not Dictionary<string, object> dict) return false;
        return dict.TryGetValue("project_id", out var pid) &&
               string.Equals(pid?.ToString(), projectId, StringComparison.Ordinal) &&
               dict.ContainsKey("data");
    }

    private static bool HasMetadata(object payload)
    {
        if (payload is not Dictionary<string, object> dict) return false;
        return dict.ContainsKey("metadata");
    }

    private static bool HasNotes(object payload, string expectedNotes)
    {
        if (payload is not Dictionary<string, object> dict) return false;
        return dict.TryGetValue("notes", out var notes) &&
               string.Equals(notes?.ToString(), expectedNotes, StringComparison.Ordinal);
    }
}
