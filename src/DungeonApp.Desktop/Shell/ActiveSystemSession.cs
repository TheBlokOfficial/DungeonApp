using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Content;
using DungeonApp.Core.Persistence;
using DungeonApp.Core.State;
using DungeonApp.Desktop.Content;

namespace DungeonApp.Desktop.Shell;

/// <summary>
/// Owns one chosen system's tab lifecycle: its System-category tabs, its Campaign-category tabs, and
/// the currently open campaign, if any. Free of Avalonia's dispatcher and the startup-warmup
/// machinery on purpose - <see cref="AppShellViewModel"/> needs both of those to drive the screen,
/// but the navigation contract with a system itself (what docs/tasks.md's "Testy" section calls "the
/// tab lifecycle on a substituted system") does not, which is what makes this type unit-testable
/// without a running application, the same way <see cref="CampaignSession"/> is.
/// <para>
/// A tab's content is built at most once and cached here until it is released - by
/// <see cref="CloseCampaign"/> (Campaign-category tabs only), or by <see cref="ReleaseAll"/>, which
/// covers every point docs/architecture.md's navigation section names as a release: returning to
/// system selection and exiting the program. Warmup never touches this cache at all - it builds and
/// releases its own throwaway content against a throwaway <see cref="CampaignTabContext"/>, so it can
/// never leave anything here for a later real open to find already built.
/// </para>
/// </summary>
public sealed class ActiveSystemSession(IGameSystem system, Func<ContentRegistry> registry, ICampaignRepository campaigns)
{
    private readonly Dictionary<string, ITabContent> _systemTabContents = [];
    private readonly Dictionary<string, ITabContent> _campaignTabContents = [];

    private CampaignSession? _openCampaign;
    private CampaignTabContext? _campaignTabContext;

    public IGameSystem System { get; } = system;

    public bool IsCampaignOpen => _openCampaign is not null;

    /// <summary>
    /// Opens <paramref name="campaign"/>, closing whatever was open before it. The campaign is
    /// assumed already loaded (by <see cref="Features.CampaignLibrary.CampaignPreparationCache"/>);
    /// this only builds the session and the tab context every Campaign-category tab factory receives.
    /// </summary>
    public void OpenCampaign(Campaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);

        CloseCampaign();

        _openCampaign = new CampaignSession(campaign, campaigns, System.StateModels);
        _campaignTabContext = new CampaignTabContext(_openCampaign, registry());
    }

    /// <summary>Releases every Campaign-category tab built for the open campaign and closes it.</summary>
    public void CloseCampaign()
    {
        foreach (var content in _campaignTabContents.Values)
        {
            content.Dispose();
        }

        _campaignTabContents.Clear();

        _openCampaign = null;
        _campaignTabContext = null;
    }

    /// <summary>The one door any change goes through, while a campaign is open.</summary>
    public Task<CampaignChangeResult> ChangeAsync(CampaignChange change)
    {
        if (_openCampaign is null)
        {
            throw new InvalidOperationException("No campaign is open.");
        }

        return _openCampaign.ChangeAsync(change);
    }

    /// <summary>
    /// Builds <paramref name="declaration"/>'s content on first call and returns the same instance on
    /// every later one - the "zawartość zakładki powstaje przy pierwszym pokazaniu" half of the tab
    /// lifecycle rule.
    /// </summary>
    public ITabContent GetOrCreateSystemTab(SystemTabDeclaration declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration);

        if (!_systemTabContents.TryGetValue(declaration.Id, out var content))
        {
            content = declaration.CreateContent(new SystemTabContext(registry()));
            _systemTabContents[declaration.Id] = content;
        }

        return content;
    }

    /// <summary>
    /// Builds <paramref name="declaration"/>'s content on first call and returns the same instance on
    /// every later one, for as long as the same campaign stays open. Throws if no campaign is open -
    /// the caller (the sidebar) never offers a locked tab to click in the first place, so reaching
    /// this without an open campaign is a shell bug, not a GM mistake to degrade gracefully from.
    /// </summary>
    public async Task<ITabContent> GetOrCreateCampaignTabAsync(CampaignTabDeclaration declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration);

        if (_campaignTabContext is null)
        {
            throw new InvalidOperationException("No campaign is open.");
        }

        if (!_campaignTabContents.TryGetValue(declaration.Id, out var content))
        {
            content = await declaration.CreateContentAsync(_campaignTabContext).ConfigureAwait(false);
            _campaignTabContents[declaration.Id] = content;
        }

        return content;
    }

    /// <summary>
    /// Releases every tab this session ever built - System-category and Campaign-category alike -
    /// and closes the open campaign. Called on "powrót do wyboru" and on program exit; safe to call
    /// more than once, since every dictionary it drains is empty after the first call.
    /// </summary>
    public void ReleaseAll()
    {
        foreach (var content in _systemTabContents.Values)
        {
            content.Dispose();
        }

        _systemTabContents.Clear();

        CloseCampaign();
    }
}
