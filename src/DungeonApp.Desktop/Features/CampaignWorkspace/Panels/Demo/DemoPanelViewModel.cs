namespace DungeonApp.Desktop.Features.CampaignWorkspace.Panels.Demo;

/// <summary>
/// Stand-in panel content. Carries no behaviour on purpose: this increment builds the window
/// mechanics, and real panels arrive with the domain.
/// </summary>
public sealed record DemoPanelViewModel(string Heading, string Body);
