using System;
using DungeonApp.Core.Modules;

namespace DungeonApp.Core.Tests.Modules;

public sealed class ModuleIdTests
{
    [Theory]
    [InlineData("core.clock")]
    [InlineData("core")]
    [InlineData("world.weather-2")]
    public void Accepts_a_plain_lowercase_identifier(string candidate)
        => Assert.True(ModuleId.TryCreate(candidate, out _));

    /// <summary>
    /// The id becomes a file name inside the campaign directory, so anything that could escape it,
    /// collide on a case-insensitive filesystem, or produce a hidden file is refused here rather
    /// than discovered when the store writes.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Core.Clock")]
    [InlineData("core/clock")]
    [InlineData("core\\clock")]
    [InlineData("../escape")]
    [InlineData(".hidden")]
    [InlineData("trailing.")]
    [InlineData("core clock")]
    public void Refuses_anything_that_would_be_unsafe_as_a_file_name(string? candidate)
    {
        Assert.False(ModuleId.IsValid(candidate));
        Assert.False(ModuleId.TryCreate(candidate, out _));
    }

    [Fact]
    public void Refuses_an_identifier_past_the_limit()
        => Assert.False(ModuleId.IsValid(new string('a', ModuleId.MaxLength + 1)));

    [Fact]
    public void Create_throws_when_the_caller_skipped_validation()
        => Assert.Throws<ArgumentException>(() => ModuleId.Create("Core.Clock"));

    [Fact]
    public void Compares_by_value()
        => Assert.Equal(ModuleId.Create("core.clock"), ModuleId.Create("core.clock"));
}
