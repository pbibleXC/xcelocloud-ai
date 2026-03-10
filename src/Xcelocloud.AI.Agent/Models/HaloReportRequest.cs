namespace Xcelocloud.AI.Agent.Models;

public enum ReportType
{
    UtilizationSummary,
    AgentCapacity,
    AgentRoles
}

public class HaloReportRequest
{
    public ReportType ReportType { get; set; }
    public List<long>? AgentIds { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
}
