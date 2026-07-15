namespace DungeonApp.Models.Campaigns.Engine.Events;

/// <summary>
/// Zdarzenie służące do realizowania Notification Feed. 
/// Moduł wysyła je, gdy chce cicho poinformować DM-a o tym, że coś zrobił autonomicznie.
/// </summary>
public class NotificationEvent : CampaignEventBase
{
    public string Message { get; init; } = string.Empty;
    public string Level { get; init; } = "Info"; // Info, Warning, Error
    
    public NotificationEvent(string message, string level = "Info")
    {
        Message = message;
        Level = level;
    }

    /// <summary>
    /// Czytelny format dla konsoli Sandbox: [HH:mm] [ModuleId] Message
    /// </summary>
    public override string ToString() => $"[{Timestamp:HH:mm}] [{SenderModuleId}] {Message}";
}
