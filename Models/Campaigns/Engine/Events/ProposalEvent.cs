using System;

namespace DungeonApp.Models.Campaigns.Engine.Events;

/// <summary>
/// Zdarzenie służące do realizowania Zasady Aktywnej Akceptacji. 
/// Zamiast modyfikować stan na twardo, moduł wysyła "Propozycję" do zatwierdzenia przez DM-a.
/// </summary>
public class ProposalEvent : CampaignEventBase
{
    /// <summary>
    /// Treść propozycji, np. "Podróż zajęła 12h. Nałożyć punkt wyczerpania na drużynę?"
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Akcja, która wykona się, gdy DM zatwierdzi propozycję.
    /// </summary>
    public Action? AcceptAction { get; init; }

    /// <summary>
    /// Akcja, która wykona się, gdy DM odrzuci propozycję. (Opcjonalna)
    /// </summary>
    public Action? RejectAction { get; init; }

    public ProposalEvent(string description, Action acceptAction, Action? rejectAction = null)
    {
        Description = description;
        AcceptAction = acceptAction;
        RejectAction = rejectAction;
    }

    /// <summary>
    /// Czytelny format dla konsoli Sandbox: [HH:mm] [PROPOZYCJA] Description
    /// </summary>
    public override string ToString() => $"[{Timestamp:HH:mm}] [PROPOZYCJA] {Description}";
}
