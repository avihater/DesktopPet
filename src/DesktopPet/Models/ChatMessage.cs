namespace DesktopPet.Models;

/// <summary>
/// A single chat message in conversation history
/// </summary>
public class ChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Role { get; set; } = "user"; // "user", "assistant", "system"
    public string Content { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public bool IsStreaming { get; set; } = false;
}
