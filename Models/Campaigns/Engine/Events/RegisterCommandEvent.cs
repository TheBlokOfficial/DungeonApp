using DungeonApp.Models.Campaigns.Engine.Commands;

namespace DungeonApp.Models.Campaigns.Engine.Events;

/// <summary>
/// Zdarzenie emitowane przez moduł podczas inicjalizacji, rejestrujące komendę w systemie autouzupełniania.
/// </summary>
/// <remarks>
/// DLACZEGO CommandNode zamiast string Description:
/// Nowy system wymaga pełnej struktury drzewa (węzły, typy argumentów, dzieci),
/// aby AutocompleteEngine mógł generować kontekstowe podpowiedzi token po tokenie.
/// String Description był artefaktem poprzedniego, uproszczonego systemu.
/// </remarks>
public class RegisterCommandEvent : CampaignEventBase
{
    /// <summary>Korzeń drzewa komendy (np. węzeł "/time" z dziećmi "set", "add").</summary>
    public CommandNode RootNode { get; }

    public string ProviderModuleId { get; }

    public RegisterCommandEvent(CommandNode rootNode, string providerModuleId)
    {
        RootNode = rootNode;
        ProviderModuleId = providerModuleId;
        SenderModuleId = providerModuleId;
    }
}
