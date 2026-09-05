using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DungeonApp.Core.Events;

namespace DungeonApp.Core.Modules;

/// <summary>
/// The modules switched on for one campaign, in a settled order.
/// <para>
/// The order is the topological one: a module is activated after everything it declared. It is
/// recorded rather than recomputed at random because it will also be the order in which modules see
/// announcements, and a save that produced one result today must produce the same one tomorrow.
/// </para>
/// </summary>
public sealed class CampaignModules
{
    private readonly Dictionary<ModuleId, ICampaignModule> _byId;

    private CampaignModules(IReadOnlyList<ICampaignModule> ordered)
    {
        Active = ordered;
        _byId = ordered.ToDictionary(module => module.Manifest.Id);
    }

    /// <summary>In activation order. Dependencies always precede the modules that declared them.</summary>
    public IReadOnlyList<ICampaignModule> Active { get; }

    /// <summary>
    /// Builds the set and refuses an impossible one. Every module is registered before any is
    /// activated, so a module may reach for another from <see cref="ICampaignModule.OnActivated"/>.
    /// </summary>
    public static CampaignModules Activate(
        IEnumerable<ICampaignModule> modules,
        CampaignEvents events)
    {
        ArgumentNullException.ThrowIfNull(modules);
        ArgumentNullException.ThrowIfNull(events);

        var requested = modules.ToArray();

        var byId = new Dictionary<ModuleId, ICampaignModule>();

        foreach (var module in requested)
        {
            if (!byId.TryAdd(module.Manifest.Id, module))
            {
                throw new ModuleActivationException(
                    ModuleActivationFailure.DuplicateModule,
                    $"Module '{module.Manifest.Id}' is switched on more than once.");
            }
        }

        foreach (var module in requested)
        {
            foreach (var required in module.Manifest.Requires)
            {
                if (!byId.ContainsKey(required))
                {
                    throw new ModuleActivationException(
                        ModuleActivationFailure.MissingDependency,
                        $"Module '{module.Manifest.Id}' requires '{required}', which is not switched on.");
                }
            }
        }

        var campaignModules = new CampaignModules(SortByDependency(requested, byId));

        foreach (var module in campaignModules.Active)
        {
            // A context each, not one shared. Subscriptions are taken here, in activation order,
            // which is what makes the order handlers run in a settled property of the campaign
            // rather than an accident.
            module.OnActivated(new ModuleContext(campaignModules, events));
        }

        return campaignModules;
    }

    /// <summary>
    /// The typed way one module reaches another. Direct and compile-checked, because a module asking
    /// a question of another is not an announcement and should not travel as one.
    /// </summary>
    public TModule Get<TModule>() where TModule : ICampaignModule =>
        TryGet<TModule>(out var module)
            ? module
            : throw new InvalidOperationException(
                $"Module '{typeof(TModule).Name}' is not switched on for this campaign. "
                + "Declare it in the manifest's Requires so the mismatch is caught up front.");

    public bool TryGet<TModule>([NotNullWhen(true)] out TModule? module) where TModule : ICampaignModule
    {
        module = Active.OfType<TModule>().FirstOrDefault();

        return module is not null;
    }

    /// <summary>Returns null for a module this campaign does not have switched on.</summary>
    public ICampaignModule? Find(ModuleId id) => _byId.GetValueOrDefault(id);

    public bool Contains(ModuleId id) => _byId.ContainsKey(id);

    /// <summary>
    /// Depth-first, so a cycle surfaces as a named error rather than a stack overflow. Ties keep the
    /// caller's order, which keeps the result stable for a given campaign configuration.
    /// </summary>
    private static IReadOnlyList<ICampaignModule> SortByDependency(
        IReadOnlyList<ICampaignModule> requested,
        Dictionary<ModuleId, ICampaignModule> byId)
    {
        var ordered = new List<ICampaignModule>(requested.Count);
        var settled = new HashSet<ModuleId>();
        var onPath = new HashSet<ModuleId>();

        foreach (var module in requested)
        {
            Visit(module);
        }

        return ordered;

        void Visit(ICampaignModule module)
        {
            var id = module.Manifest.Id;

            if (settled.Contains(id))
            {
                return;
            }

            if (!onPath.Add(id))
            {
                throw new ModuleActivationException(
                    ModuleActivationFailure.CircularDependency,
                    $"Module '{id}' takes part in a dependency loop, so no activation order exists.");
            }

            foreach (var required in module.Manifest.Requires)
            {
                Visit(byId[required]);
            }

            onPath.Remove(id);
            settled.Add(id);
            ordered.Add(module);
        }
    }
}
