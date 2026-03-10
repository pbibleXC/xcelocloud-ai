using System.ClientModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using OpenAI;
using Xcelocloud.AI.Agent.Models;

namespace Xcelocloud.AI.Agent.Orchestrators;

public interface IHaloChatOrchestrator
{
    Task<HaloChatResponse> ChatAsync(HaloChatRequest request, CancellationToken cancellationToken = default);
}

internal sealed class HaloChatOrchestrator : IHaloChatOrchestrator
{
    private readonly McpServerOptions _mcpOptions;
    private readonly OpenRouterOptions _openRouterOptions;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<HaloChatOrchestrator> _logger;

    public HaloChatOrchestrator(
        IOptions<McpServerOptions> mcpOptions,
        IOptions<OpenRouterOptions> openRouterOptions,
        ILoggerFactory loggerFactory)
    {
        _mcpOptions = mcpOptions.Value;
        _openRouterOptions = openRouterOptions.Value;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<HaloChatOrchestrator>();
    }

    public async Task<HaloChatResponse> ChatAsync(HaloChatRequest request, CancellationToken cancellationToken = default)
    {
        var mcpEndpoint = new Uri($"{_mcpOptions.BaseUrl.TrimEnd('/')}/runtime/webhooks/mcp");

        await using var mcpClient = await McpClient.CreateAsync(
            new HttpClientTransport(new HttpClientTransportOptions
            {
                Endpoint = mcpEndpoint,
                Name = "Xcelocloud.AI.Mcp",
                AdditionalHeaders = new Dictionary<string, string>
                {
                    ["x-functions-key"] = _mcpOptions.FunctionKey
                }
            }, _loggerFactory),
            cancellationToken: cancellationToken);

        var tools = await mcpClient.ListToolsAsync(cancellationToken: cancellationToken);
        _logger.LogDebug("Loaded {ToolCount} MCP tools from {Endpoint}", tools.Count, mcpEndpoint);

        IChatClient chatClient = new OpenAIClient(
                new ApiKeyCredential(_openRouterOptions.ApiKey),
                new OpenAIClientOptions { Endpoint = new Uri("https://openrouter.ai/api/v1") })
            .GetChatClient(_openRouterOptions.ModelId)
            .AsIChatClient();

        var agent = chatClient.AsAIAgent(
            AgentInformation.HaloChat.SystemPrompt,
            AgentInformation.HaloChat.AgentName,
            description: null,
            tools: tools.Cast<AITool>().ToList(),
            loggerFactory: _loggerFactory,
            services: null);

        var session = await agent.CreateSessionAsync(cancellationToken);
        var userMessage = BuildUserMessage(request);
        _logger.LogDebug("Running HaloChatAgent with message length: {Length}", userMessage.Length);

        var response = await agent.RunAsync(userMessage, session, options: null, cancellationToken);

        return new HaloChatResponse { Message = response.Text };
    }

    internal static string BuildUserMessage(HaloChatRequest request)
    {
        if (request.History is not { Count: > 0 })
            return request.Message;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Conversation history:");
        foreach (var turn in request.History)
        {
            var role = turn.Role.Equals("user", StringComparison.OrdinalIgnoreCase) ? "User" : "Assistant";
            sb.AppendLine($"[{role}]: {turn.Content}");
        }
        sb.AppendLine();
        sb.Append($"Current message: {request.Message}");
        return sb.ToString();
    }
}
