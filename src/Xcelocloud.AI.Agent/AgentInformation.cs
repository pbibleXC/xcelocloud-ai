using System.Runtime.CompilerServices;
using Xcelocloud.AI.Agent.Models;

[assembly: InternalsVisibleTo("Xcelocloud.AI.Agent.Tests")]

namespace Xcelocloud.AI.Agent;

internal static class AgentInformation
{
    public const string McpClientName = "Mcp";

    internal static class HaloChat
    {
        public const string AgentName = "HaloChatAgent";
        public const string Description =
            "An AI assistant that answers questions about Halo PSA by querying the Halo MCP tools.";
        public const string SystemPrompt =
            """
            You are a helpful Halo PSA assistant with access to Halo data via tools.
            Answer questions accurately, using the available tools to retrieve data when needed.
            Be concise and conversational. If you cannot find the requested data, say so clearly.
            Format your responses using Markdown. Use tables for lists of records, bold for emphasis, and code blocks where appropriate.
            Do not wrap your answer in JSON.
            """;
    }

    internal static class HaloReporting
    {
        public const string AgentName = "HaloReportingAgent";
        public const string Description =
            "An AI agent that generates structured Halo System reports including utilization summaries, " +
            "agent capacity analysis, and role assignments by querying the Halo MCP tools.";
        public const string SystemPrompt =
            """
            You are a Halo PSA reporting assistant. Use the available Halo tools to retrieve the requested data.
            Always respond with a JSON object in the following format:
            {
              "summary": "<concise human-readable summary of the findings>",
              "data": <the raw structured data retrieved, or null if unavailable>
            }
            Do not include any text outside the JSON object.
            """;
    }

    internal static class PromptTemplates
    {
        public static string ForUtilizationSummary(HaloReportRequest request)
        {
            var agentIds = request.AgentIds is { Count: > 0 }
                ? string.Join(",", request.AgentIds)
                : string.Empty;
            var start = request.StartDate?.ToString("O") ?? string.Empty;
            var end = request.EndDate?.ToString("O") ?? string.Empty;

            return $"Generate a utilization summary report. Agent IDs filter: '{agentIds}'. " +
                   $"Date range: {start} to {end}. " +
                   "Retrieve agent utilization data and summarize billable hours, utilization rates, and any notable patterns.";
        }

        public static string ForAgentCapacity(HaloReportRequest request)
        {
            var agentIds = request.AgentIds is { Count: > 0 }
                ? string.Join(",", request.AgentIds)
                : string.Empty;
            var start = request.StartDate?.ToString("O") ?? string.Empty;
            var end = request.EndDate?.ToString("O") ?? string.Empty;

            return $"Generate an agent capacity report. Agent IDs filter: '{agentIds}'. " +
                   $"Date range: {start} to {end}. " +
                   "Retrieve agent list and utilization data. Report on capacity, billable target vs actual hours, and leave hours.";
        }

        public static string ForAgentRoles(HaloReportRequest request)
        {
            var agentIds = request.AgentIds is { Count: > 0 }
                ? string.Join(",", request.AgentIds)
                : string.Empty;

            return $"Generate an agent roles report. Agent IDs filter: '{agentIds}'. " +
                   "Retrieve all agents and their assigned roles. Summarize the role distribution and flag any agents without assigned roles.";
        }
    }
}
