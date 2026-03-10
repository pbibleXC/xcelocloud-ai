using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Xcelocloud.AI.Agent.Models;
using Xcelocloud.AI.Agent.Orchestrators;

namespace Xcelocloud.AI.Agent.Agents;

public class HaloReportingAgent
{
    private readonly IHaloReportingOrchestrator _orchestrator;
    private readonly ILogger<HaloReportingAgent> _logger;

    public HaloReportingAgent(IHaloReportingOrchestrator orchestrator, ILogger<HaloReportingAgent> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    [Function("HaloReport")]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "halo/reports")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        HaloReportRequest? reportRequest;

        try
        {
            reportRequest = await request.ReadFromJsonAsync<HaloReportRequest>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize HaloReportRequest.");
            return new BadRequestObjectResult("Invalid request body.");
        }

        if (reportRequest is null)
        {
            return new BadRequestObjectResult("Request body is required.");
        }

        _logger.LogInformation("Processing Halo report: {ReportType}", reportRequest.ReportType);

        var response = await _orchestrator.ProcessAsync(reportRequest, cancellationToken);

        return new OkObjectResult(response);
    }
}
