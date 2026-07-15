using System;

namespace DungeonApp.Models.Campaigns.Engine.Events;

/// <summary>
/// Bazowa abstrakcja dla wszystkich wiadomości wymienianych w systemie kampanii.
/// </summary>
public abstract class CampaignEventBase
{
    /// <summary>
    /// Stempel czasowy utworzenia zdarzenia.
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.Now;

    /// <summary>
    /// Identyfikator modułu, który wygenerował to zdarzenie.
    /// </summary>
    public string SenderModuleId { get; init; } = string.Empty;
}
