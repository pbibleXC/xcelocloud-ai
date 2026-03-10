using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Xcelocloud.AI.Mcp.Tests")]

namespace Xcelocloud.AI.Mcp
{
    internal static class ToolsInformation
    {
        internal static class Halo
        {
            public const string ToolName = "Halo";
            public const string Description =
                "Retrieves HaloPSA reporting data for a specific named report or information type. " +
                "Use this to query named Halo data sets such as utilization summaries or custom reports.";
            public const string PropertyName = "name";
            public const string PropertyDescription = "The name of the Halo report or information type to retrieve (e.g. 'utilization', 'summary').";
        }

        internal static class GetAgents
        {
            public const string ToolName = "GetAgents";
            public const string Description =
                "Returns a list of all active HaloPSA agents (technicians/staff). " +
                "Each agent includes their Id, name, email address, and team. " +
                "Use this to look up agent names, IDs for assignment, or to enumerate available technicians.";
        }   

        internal static class GetAgentRoles
        {
            public const string ToolName = "GetAgentRoles";
            public const string Description =
                "Returns a list of all active HaloPSA agent roles (the functions they serve). " +
                "Each agent role includes the Agent Id, Agent Name, Role Id, and Role Name. " +
                "Use this to look up agent roles associated with agents, or agents associated with roles.";
        }   

        internal static class GetAgentUtilization
        {
            public const string ToolName = "GetAgentUtilization";
            public const string Description =
                "Returns utilization metrics for HaloPSA agents over a specified time period. " +
                "Each record includes the Agent Id, Agent Name, Billable Hours, Non-Billable Hours, Billable Target Hours, Leave Hours, and Utilization Rate (%). " +
                "Use this to assess how effectively agents are being utilized during a given time frame.";

            public const string AgentIdsPropertyName = "agentIds";
            public const string AgentIdsPropertyDescription = "Optional comma-separated list of agent IDs to filter by (e.g. '1,2,3'). Pass an empty string to retrieve utilization for all agents.";

            public const string StartDatePropertyName = "startDate";
            public const string StartDatePropertyDescription = "The start of the date range in ISO 8601 format (e.g. '2026-02-01T00:00:00Z'). Defaults to 30 days ago if not provided.";

            public const string EndDatePropertyName = "endDate";
            public const string EndDatePropertyDescription = "The end of the date range in ISO 8601 format (e.g. '2026-03-01T00:00:00Z'). Defaults to today if not provided.";
        }
    }
}
