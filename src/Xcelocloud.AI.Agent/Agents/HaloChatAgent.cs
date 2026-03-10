using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Xcelocloud.AI.Agent.Models;
using Xcelocloud.AI.Agent.Orchestrators;

namespace Xcelocloud.AI.Agent.Agents;

public class HaloChatAgent
{
    private readonly IHaloChatOrchestrator _orchestrator;
    private readonly ILogger<HaloChatAgent> _logger;

    public HaloChatAgent(IHaloChatOrchestrator orchestrator, ILogger<HaloChatAgent> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    [Function("HaloChat")]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "halo/chat")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        HaloChatRequest? chatRequest;

        try
        {
            chatRequest = await request.ReadFromJsonAsync<HaloChatRequest>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize HaloChatRequest.");
            return new BadRequestObjectResult("Invalid request body.");
        }

        if (chatRequest is null || string.IsNullOrWhiteSpace(chatRequest.Message))
        {
            return new BadRequestObjectResult("A non-empty Message is required.");
        }

        _logger.LogInformation("Processing Halo chat message (length: {Length})", chatRequest.Message.Length);

        var response = await _orchestrator.ChatAsync(chatRequest, cancellationToken);

        return new OkObjectResult(response);
    }
}
