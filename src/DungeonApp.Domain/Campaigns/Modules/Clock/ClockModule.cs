using DungeonApp.Domain.Campaigns.Events;

namespace DungeonApp.Domain.Campaigns.Modules.Clock;

/// <summary>
/// Optional module which maintains elapsed world time for one campaign.
/// </summary>
public sealed class ClockModule : ICampaignModule, ICampaignTimeSource
{
    public static ModuleId Id { get; } = new("core.clock");

    public ClockModule(CampaignTime? currentTime = null)
    {
        CurrentTime = currentTime ?? CampaignTime.Start;
    }

    public CampaignModuleDescriptor Descriptor { get; } = new(Id, stateVersion: 1);

    public CampaignTime CurrentTime { get; private set; }

    public ICampaignModule CreateWorkingCopy() => new ClockModule(CurrentTime);

    public void Handle(CampaignCommand command, CampaignModuleContext context)
    {
        if (command is not AdvanceWorldTime advanceWorldTime)
        {
            throw new InvalidOperationException($"Clock module cannot handle '{command.GetType().Name}'.");
        }

        var newTime = CurrentTime.AdvanceBy(advanceWorldTime.Elapsed);
        context.Publish(
            new WorldTimeAdvanced(CurrentTime, newTime, advanceWorldTime.Elapsed, advanceWorldTime.Reason),
            newTime);
    }

    public void ReactTo(CampaignEvent campaignEvent, CampaignModuleContext context)
    {
        if (campaignEvent.Payload is not WorldTimeAdvanced worldTimeAdvanced)
        {
            return;
        }

        if (worldTimeAdvanced.From != CurrentTime)
        {
            throw new InvalidOperationException("World time event does not follow the current clock state.");
        }

        CurrentTime = worldTimeAdvanced.To;
    }
}
