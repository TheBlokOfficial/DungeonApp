using System;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Content;
using DungeonApp.Core.Tests.Fakes;

namespace DungeonApp.Core.Tests.Campaigns;

public sealed class CreateCampaignTests
{
    private static readonly DateTimeOffset Moment = new(2026, 8, 27, 18, 30, 0, TimeSpan.Zero);
    private static readonly ContentId SystemId = ContentId.Create("test-system");

    private readonly InMemoryCampaignRepository _repository = new();
    private readonly CreateCampaign _createCampaign;

    public CreateCampaignTests()
        => _createCampaign = new CreateCampaign(_repository, new FixedTimeProvider(Moment));

    [Fact]
    public async Task Stores_the_campaign_it_created()
    {
        var created = await _createCampaign.ExecuteAsync("Kroniki Doliny", SystemId, []);

        var stored = await _repository.GetAsync(created.Id, []);

        Assert.Same(created, stored);
        Assert.Equal("Kroniki Doliny", created.Name.Value);
        Assert.Equal(Moment, created.CreatedAt);
    }

    /// <summary>Creation stamps the active system onto the campaign, never to be asked again after this.</summary>
    [Fact]
    public async Task Stamps_the_campaign_with_the_active_system()
    {
        var created = await _createCampaign.ExecuteAsync("Kroniki Doliny", SystemId, []);

        Assert.Equal(SystemId, created.SystemId);
    }

    /// <summary>
    /// The GM is free to run two campaigns under one title. Nothing keys off the name, so nothing
    /// may collide because of it.
    /// </summary>
    [Fact]
    public async Task Treats_a_repeated_name_as_a_separate_campaign()
    {
        var first = await _createCampaign.ExecuteAsync("Kroniki Doliny", SystemId, []);
        var second = await _createCampaign.ExecuteAsync("Kroniki Doliny", SystemId, []);

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(2, _repository.SaveCount);
    }

    /// <summary>A rejected name must not leave a half-created campaign behind.</summary>
    [Fact]
    public async Task Leaves_the_store_untouched_when_the_name_is_rejected()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _createCampaign.ExecuteAsync("   ", SystemId, []));

        Assert.Equal(0, _repository.SaveCount);
    }
}
