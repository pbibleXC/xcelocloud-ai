using System.ClientModel;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using OpenAI;
using Xcelocloud.AI.Agent.Models;

namespace Xcelocloud.AI.Agent.Orchestrators;

public interface IHaloReportingOrchestrator
{
    Task<HaloReportResponse> ProcessAsync(HaloReportRequest request, CancellationToken cancellationToken = default);
}

internal sealed class HaloReportingOrchestrator : IHaloReportingOrchestrator
{
    private readonly McpServerOptions _mcpOptions;
    private readonly OpenRouterOptions _openRouterOptions;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<HaloReportingOrchestrator> _logger;

    public HaloReportingOrchestrator(
        IOptions<McpServerOptions> mcpOptions,
        IOptions<OpenRouterOptions> openRouterOptions,
        ILoggerFactory loggerFactory)
    {
        _mcpOptions = mcpOptions.Value;
        _openRouterOptions = openRouterOptions.Value;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<HaloReportingOrchestrator>();
    }

    public async Task<HaloReportResponse> ProcessAsync(HaloReportRequest request, CancellationToken cancellationToken = default)
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
            AgentInformation.HaloReporting.SystemPrompt,
            AgentInformation.HaloReporting.AgentName,
            description: null,
            tools: tools.Cast<AITool>().ToList(),
            loggerFactory: _loggerFactory,
            services: null);

        var session = await agent.CreateSessionAsync(cancellationToken);
        var userMessage = BuildUserMessage(request);
        _logger.LogDebug("Running HaloReportingAgent with message: {Message}", userMessage);

        var response = await agent.RunAsync(userMessage, session, options: null, cancellationToken);

        return ParseResponse(response.Text, request.ReportType);
    }

    internal static string BuildUserMessage(HaloReportRequest request) => request.ReportType switch
    {
        ReportType.UtilizationSummary => AgentInformation.PromptTemplates.ForUtilizationSummary(request),
        ReportType.AgentCapacity      => AgentInformation.PromptTemplates.ForAgentCapacity(request),
        ReportType.AgentRoles         => AgentInformation.PromptTemplates.ForAgentRoles(request),
        _ => throw new ArgumentOutOfRangeException(nameof(request), request.ReportType, "Unknown report type.")
    };

    internal static HaloReportResponse ParseResponse(string text, ReportType reportType)
    {
        var generatedAt = DateTimeOffset.UtcNow;

        try
        {
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;

            var summary = root.TryGetProperty("summary", out var summaryEl)
                ? summaryEl.GetString() ?? string.Empty
                : string.Empty;

            JsonElement? data = null;
            if (root.TryGetProperty("data", out var dataEl) && dataEl.ValueKind != JsonValueKind.Null)
                data = dataEl.Clone();

            return new HaloReportResponse
            {
                Summary = summary,
                ReportType = reportType,
                GeneratedAt = generatedAt,
                Data = data
            };
        }
        catch (JsonException)
        {
            return new HaloReportResponse
            {
                Summary = text,
                ReportType = reportType,
                GeneratedAt = generatedAt,
                Data = null
            };
        }
    }
}
