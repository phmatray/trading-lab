namespace TradingStrat.Web.Models.State;

public class ChatHistory
{
    public List<ChatMessage> Messages { get; set; } = new();
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class ChatMessage
{
    public string Content { get; set; } = string.Empty;
    public bool IsUser { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
