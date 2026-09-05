using System;
using DungeonApp.Core.DataBlocks;

namespace DungeonApp.Core.Tests.DataBlocks;

public sealed class DataBlockIdTests
{
    [Theory]
    [InlineData("party.roster")]
    [InlineData("party")]
    [InlineData("world.weather-2")]
    public void Accepts_a_plain_lowercase_identifier(string candidate)
        => Assert.True(DataBlockId.TryCreate(candidate, out _));

    /// <summary>
    /// The id becomes a file name inside the campaign directory, so anything that could escape it,
    /// collide on a case-insensitive filesystem, or produce a hidden file is refused here rather
    /// than discovered when the store writes.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Party.Roster")]
    [InlineData("party/roster")]
    [InlineData("party\\roster")]
    [InlineData("../escape")]
    [InlineData(".hidden")]
    [InlineData("trailing.")]
    [InlineData("party roster")]
    public void Refuses_anything_that_would_be_unsafe_as_a_file_name(string? candidate)
    {
        Assert.False(DataBlockId.IsValid(candidate));
        Assert.False(DataBlockId.TryCreate(candidate, out _));
    }

    [Fact]
    public void Refuses_an_identifier_past_the_limit()
        => Assert.False(DataBlockId.IsValid(new string('a', DataBlockId.MaxLength + 1)));

    [Fact]
    public void Create_throws_when_the_caller_skipped_validation()
        => Assert.Throws<ArgumentException>(() => DataBlockId.Create("Party.Roster"));

    [Fact]
    public void Compares_by_value()
        => Assert.Equal(DataBlockId.Create("party.roster"), DataBlockId.Create("party.roster"));
}
