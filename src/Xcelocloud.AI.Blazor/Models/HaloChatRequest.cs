namespace Xcelocloud.AI.Blazor.Models;

/// <summary>Client-side mirror of the AI.Agent HaloChatRequest.</summary>
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
