using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text;
using System.Text.Json;
using Xcelocloud.AI.Agent.Agents;
using Xcelocloud.AI.Agent.Models;
using Xcelocloud.AI.Agent.Orchestrators;

namespace Xcelocloud.AI.Agent.Tests;

[TestFixture]
public class HaloReportingAgentTests
{
    private Mock<IHaloReportingOrchestrator> _orchestratorMock = null!;
    private HaloReportingAgent _agent = null!;

    [SetUp]
    public void SetUp()
    {
        _orchestratorMock = new Mock<IHaloReportingOrchestrator>();
        _agent = new HaloReportingAgent(
            _orchestratorMock.Object,
            NullLogger<HaloReportingAgent>.Instance);
    }

    [Test]
    public async Task RunAsync_ValidRequest_Returns200WithResponse()
    {
        var expected = new HaloReportResponse
        {
            Summary = "3 agents recorded.",
            ReportType = ReportType.UtilizationSummary,
            GeneratedAt = DateTimeOffset.UtcNow
        };

        _orchestratorMock
            .Setup(o => o.ProcessAsync(It.IsAny<HaloReportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var request = MakeRequest(new HaloReportRequest { ReportType = ReportType.UtilizationSummary });

        var result = await _agent.RunAsync(request, CancellationToken.None);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.SameAs(expected));
    }

    [Test]
    public async Task RunAsync_EmptyBody_Returns400()
    {
        var request = MakeRequest(null);

        var result = await _agent.RunAsync(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        _orchestratorMock.Verify(
            o => o.ProcessAsync(It.IsAny<HaloReportRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task RunAsync_InvalidJson_Returns400()
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/json";
        var body = Encoding.UTF8.GetBytes("not json at all");
        context.Request.Body = new MemoryStream(body);

        var result = await _agent.RunAsync(context.Request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static HttpRequest MakeRequest(HaloReportRequest? payload)
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/json";

        if (payload is not null)
        {
            var json = JsonSerializer.Serialize(payload);
            var bytes = Encoding.UTF8.GetBytes(json);
            context.Request.Body = new MemoryStream(bytes);
        }
        else
        {
            context.Request.Body = new MemoryStream([]);
        }

        return context.Request;
    }
}
