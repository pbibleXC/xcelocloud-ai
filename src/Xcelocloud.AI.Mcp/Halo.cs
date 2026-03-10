using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using Xcelocloud.Service.Shared.Helpers;

namespace Xcelocloud.AI.Mcp;

public class Halo
{
    private readonly IHaloQueryHelper _haloQueryHelper;
    private readonly ILogger<Halo> _logger;

    public Halo(IHaloQueryHelper haloQueryHelper, ILogger<Halo> logger)
    {
        _haloQueryHelper = haloQueryHelper;
        _logger = logger;
    }

    /// <summary>
    /// Returns all HaloPSA agents as a JSON array.
    /// Each element contains: Id (int), Name (string), Email (string), Job Title (string), Disabled (bool).
    /// </summary>
    [Function(nameof(GetAgents))]
    public async Task<string> GetAgents(
        [McpToolTrigger(ToolsInformation.GetAgents.ToolName, 
                        ToolsInformation.GetAgents.Description)] 
                        ToolInvocationContext context)
    {
        _logger.LogInformation("Retrieving Halo Agents...");
        try
        {
            var agents = await _haloQueryHelper.RetrieveAgentList();

            return JsonSerializer.Serialize(new
            {
                description = "A list of HaloPSA agents (support staff/technicians). Each agent has an Id, Name, Email, Job Title, and Disabled status.",
                count = agents?.Count ?? 0,
                agents
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve Halo agents");
            return JsonSerializer.Serialize(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Returns all HaloPSA agent roles as a JSON array.
    /// Each element contains: Agent Id (int), Agent Name (string), Role Id (int), Role Name (string).
    /// </summary>
    [Function(nameof(GetAgentRoles))]
    public async Task<string> GetAgentRoles(
        [McpToolTrigger(ToolsInformation.GetAgentRoles.ToolName, 
                        ToolsInformation.GetAgentRoles.Description)] 
                        ToolInvocationContext context)
    {
        _logger.LogInformation("Retrieving Halo Agent Roles...");
        try
        {
            var agentRoles = await _haloQueryHelper.RetrieveAgentRoleList();

            return JsonSerializer.Serialize(new
            {
                description = "A list of HaloPSA agent roles (the functions they serve). Each role has an Agent Id, Agent Name, Role Id, and Role Name.",
                count = agentRoles?.Count ?? 0,
                agentRoles
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve Halo agent roles");
            return JsonSerializer.Serialize(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Returns all HaloPSA agent utilization metrics as a JSON array.
    /// Each element contains: Agent Id (int), Agent Name (string), Billable Hours (decimal), Non-Billable Hours (decimal), Billable Target Hours (decimal), Leave Hours (decimal), Utilization Rate (decimal).
    /// </summary>
    [Function(nameof(GetAgentUtilization))]
    public async Task<string> GetAgentUtilization(
        [McpToolTrigger(ToolsInformation.GetAgentUtilization.ToolName, 
                        ToolsInformation.GetAgentUtilization.Description)] 
                        ToolInvocationContext context)
    {
        _logger.LogInformation("Retrieving Halo Agent Utilization...");
        try
        {
            var args = context?.Arguments ?? [];

            var agentIds = args.TryGetValue(ToolsInformation.GetAgentUtilization.AgentIdsPropertyName, out var agentIdsRaw)
                ? (agentIdsRaw?.ToString() ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => long.TryParse(s, out var id) ? id : 0L)
                    .Where(id => id > 0)
                    .ToList()
                : [];

            var startDate = args.TryGetValue(ToolsInformation.GetAgentUtilization.StartDatePropertyName, out var startRaw)
                && DateTimeOffset.TryParse(startRaw?.ToString(), out var parsedStart)
                ? parsedStart
                : DateTimeOffset.UtcNow.AddDays(-30);

            var endDate = args.TryGetValue(ToolsInformation.GetAgentUtilization.EndDatePropertyName, out var endRaw)
                && DateTimeOffset.TryParse(endRaw?.ToString(), out var parsedEnd)
                ? parsedEnd
                : DateTimeOffset.UtcNow;

            var agentUtilization = await _haloQueryHelper.RetrieveAgentUtilizationList(agentIds, startDate, endDate);

            return JsonSerializer.Serialize(new
            {
                description = "A list of HaloPSA agent utilization metrics. Each record includes the Agent Id, Agent Name, Billable Hours, Non-Billable Hours, Billable Target Hours, Leave Hours, and Utilization Rate (%).",
                count = agentUtilization?.Count ?? 0,
                agentUtilization
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve Halo agent utilization metrics");
            return JsonSerializer.Serialize(new { success = false, error = ex.Message });
        }
    }
}
