using DungeonApp.Domain.Campaigns;
using DungeonApp.Domain.Campaigns.Modules;

namespace DungeonApp.Application.Campaigns;

/// <summary>
/// Application-facing registry of module factories. Composition registers the available modules
/// once; use cases only request a module by its stable identifier.
/// </summary>
public sealed class CampaignModuleCatalog
{
    private readonly IReadOnlyDictionary<ModuleId, ICampaignModuleFactory> _factories;

    public CampaignModuleCatalog(IEnumerable<ICampaignModuleFactory> factories)
    {
        ArgumentNullException.ThrowIfNull(factories);
        _factories = factories.ToDictionary(factory => factory.ModuleId);
    }

    public IReadOnlyCollection<ICampaignModuleFactory> Factories => _factories.Values.ToList().AsReadOnly();

    public ICampaignModule Create(ModuleId moduleId) =>
        (_factories.GetValueOrDefault(moduleId) ?? throw new CampaignModuleUnavailableException(moduleId)).Create();
}
