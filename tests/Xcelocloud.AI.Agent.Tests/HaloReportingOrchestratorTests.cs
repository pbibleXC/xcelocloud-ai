using Xcelocloud.AI.Agent.Models;
using Xcelocloud.AI.Agent.Orchestrators;

namespace Xcelocloud.AI.Agent.Tests;

[TestFixture]
public class HaloReportingOrchestratorTests
{
    // ── BuildUserMessage ──────────────────────────────────────────────────────

    [Test]
    public void BuildUserMessage_UtilizationSummary_ContainsKeyTerms()
    {
        var request = new HaloReportRequest
        {
            ReportType = ReportType.UtilizationSummary,
            AgentIds = [1, 2, 3],
            StartDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            EndDate = new DateTimeOffset(2026, 1, 31, 0, 0, 0, TimeSpan.Zero)
        };

        var message = HaloReportingOrchestrator.BuildUserMessage(request);

        Assert.That(message, Does.Contain("utilization summary"));
        Assert.That(message, Does.Contain("1,2,3"));
        Assert.That(message, Does.Contain("2026-01-01"));
    }

    [Test]
    public void BuildUserMessage_AgentCapacity_ContainsKeyTerms()
    {
        var request = new HaloReportRequest
        {
            ReportType = ReportType.AgentCapacity,
            StartDate = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero),
            EndDate = new DateTimeOffset(2026, 2, 28, 0, 0, 0, TimeSpan.Zero)
        };

        var message = HaloReportingOrchestrator.BuildUserMessage(request);

        Assert.That(message, Does.Contain("capacity"));
        Assert.That(message, Does.Contain("2026-02-01"));
    }

    [Test]
    public void BuildUserMessage_AgentRoles_ContainsKeyTerms()
    {
        var request = new HaloReportRequest
        {
            ReportType = ReportType.AgentRoles,
            AgentIds = [42]
        };

        var message = HaloReportingOrchestrator.BuildUserMessage(request);

        Assert.That(message, Does.Contain("roles"));
        Assert.That(message, Does.Contain("42"));
    }

    [Test]
    public void BuildUserMessage_UnknownReportType_ThrowsArgumentOutOfRange()
    {
        var request = new HaloReportRequest { ReportType = (ReportType)99 };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            HaloReportingOrchestrator.BuildUserMessage(request));
    }

    [Test]
    public void BuildUserMessage_NullAgentIds_ProducesEmptyFilter()
    {
        var request = new HaloReportRequest
        {
            ReportType = ReportType.UtilizationSummary,
            AgentIds = null
        };

        var message = HaloReportingOrchestrator.BuildUserMessage(request);

        Assert.That(message, Does.Contain("''"));
    }

    // ── ParseResponse ─────────────────────────────────────────────────────────

    [Test]
    public void ParseResponse_ValidJson_ExtractsSummaryAndData()
    {
        const string json = """{"summary":"All agents healthy","data":{"count":5}}""";

        var result = HaloReportingOrchestrator.ParseResponse(json, ReportType.UtilizationSummary);

        Assert.That(result.Summary, Is.EqualTo("All agents healthy"));
        Assert.That(result.ReportType, Is.EqualTo(ReportType.UtilizationSummary));
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.GeneratedAt, Is.GreaterThan(DateTimeOffset.UtcNow.AddSeconds(-5)));
    }

    [Test]
    public void ParseResponse_JsonWithNullData_SetsDataToNull()
    {
        const string json = """{"summary":"No data found","data":null}""";

        var result = HaloReportingOrchestrator.ParseResponse(json, ReportType.AgentRoles);

        Assert.That(result.Summary, Is.EqualTo("No data found"));
        Assert.That(result.Data, Is.Null);
    }

    [Test]
    public void ParseResponse_InvalidJson_FallsBackToPlainTextSummary()
    {
        const string plainText = "Here is a summary of the agents.";

        var result = HaloReportingOrchestrator.ParseResponse(plainText, ReportType.AgentCapacity);

        Assert.That(result.Summary, Is.EqualTo(plainText));
        Assert.That(result.Data, Is.Null);
        Assert.That(result.ReportType, Is.EqualTo(ReportType.AgentCapacity));
    }

    [Test]
    public void ParseResponse_JsonMissingSummaryProperty_ReturnsEmptySummary()
    {
        const string json = """{"data":{"items":[]}}""";

        var result = HaloReportingOrchestrator.ParseResponse(json, ReportType.UtilizationSummary);

        Assert.That(result.Summary, Is.EqualTo(string.Empty));
        Assert.That(result.Data, Is.Not.Null);
    }
}

