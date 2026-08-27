using System;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Tests.Fakes;

namespace DungeonApp.Core.Tests.Campaigns;

public sealed class CampaignTests
{
    private static readonly DateTimeOffset Moment = new(2026, 8, 27, 18, 30, 0, TimeSpan.Zero);

    /// <summary>
    /// Identity is the whole reason CampaignId exists apart from the name. Two campaigns sharing a
    /// title must still be two campaigns.
    /// </summary>
    [Fact]
    public void Gives_every_campaign_its_own_identity()
    {
        var clock = new FixedTimeProvider(Moment);
        var name = CampaignName.Create("Kroniki Doliny");

        var first = Campaign.Create(name, clock);
        var second = Campaign.Create(name, clock);

        Assert.NotEqual(first.Id, second.Id);
        Assert.NotEqual(default, first.Id);
    }

    [Fact]
    public void Records_the_moment_from_the_injected_clock()
    {
        var campaign = Campaign.Create(CampaignName.Create("Kroniki Doliny"), new FixedTimeProvider(Moment));

        Assert.Equal(Moment, campaign.CreatedAt);
    }
}
