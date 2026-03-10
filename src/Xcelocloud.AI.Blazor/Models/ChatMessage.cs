namespace Xcelocloud.AI.Blazor.Models;

public enum ChatRole
{
    User,
    Assistant
}

public class ChatMessage
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public ChatRole Role { get; init; }
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;
    public bool IsLoading { get; set; }
}
