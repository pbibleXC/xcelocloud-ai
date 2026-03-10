using System.Text.Json;

namespace Xcelocloud.AI.Agent.Models;

public class HaloReportResponse
{
    public string Summary { get; set; } = string.Empty;
    public ReportType ReportType { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }
    public JsonElement? Data { get; set; }
}
