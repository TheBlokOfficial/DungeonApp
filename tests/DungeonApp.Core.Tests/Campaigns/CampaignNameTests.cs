using System;
using DungeonApp.Core.Campaigns;

namespace DungeonApp.Core.Tests.Campaigns;

public sealed class CampaignNameTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Rejects_a_name_with_no_content(string? candidate)
    {
        Assert.Equal(CampaignNameError.Empty, CampaignName.Validate(candidate));
        Assert.False(CampaignName.TryCreate(candidate, out _));
    }

    [Fact]
    public void Rejects_a_name_past_the_limit()
    {
        var tooLong = new string('a', CampaignName.MaxLength + 1);

        Assert.Equal(CampaignNameError.TooLong, CampaignName.Validate(tooLong));
    }

    [Fact]
    public void Accepts_a_name_exactly_at_the_limit()
    {
        var atLimit = new string('a', CampaignName.MaxLength);

        Assert.Equal(CampaignNameError.None, CampaignName.Validate(atLimit));
    }

    /// <summary>
    /// The limit is measured after trimming, so padding a name with spaces cannot smuggle it past
    /// the check or fail a name that is legal once tidied.
    /// </summary>
    [Fact]
    public void Trims_before_measuring_and_before_storing()
    {
        var padded = $"  {new string('a', CampaignName.MaxLength)}  ";

        Assert.True(CampaignName.TryCreate(padded, out var name));
        Assert.Equal(CampaignName.MaxLength, name.Value.Length);
        Assert.Equal("Kroniki Doliny", CampaignName.Create("  Kroniki Doliny  ").Value);
    }

    [Fact]
    public void Create_throws_when_the_caller_skipped_validation()
        => Assert.Throws<ArgumentException>(() => CampaignName.Create("  "));
}
