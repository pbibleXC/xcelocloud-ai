namespace Xcelocloud.AI.Agent.Models;

public class HaloChatRequest
{
    public string Message { get; set; } = string.Empty;
    public List<HaloChatTurn> History { get; set; } = [];
}

public class HaloChatTurn
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
