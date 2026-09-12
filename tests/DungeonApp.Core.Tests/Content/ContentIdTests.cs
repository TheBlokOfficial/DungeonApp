using System;
using DungeonApp.Core.Content;

namespace DungeonApp.Core.Tests.Content;

public sealed class ContentIdTests
{
    [Theory]
    [InlineData("dnd5e")]
    [InlineData("potwor")]
    [InlineData("content-pack.v2")]
    public void Accepts_a_plain_lowercase_identifier(string candidate)
        => Assert.True(ContentId.TryCreate(candidate, out _));

    /// <summary>
    /// Same rules as <see cref="DataBlocks.DataBlockId"/>, and for the same reason: an id must never
    /// be able to escape a path, collide on a case-insensitive filesystem, or be misread as part of a
    /// <c>"pack:id"</c> reference - which is exactly why a colon is refused here too.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Dnd5e")]
    [InlineData("pack/id")]
    [InlineData("pack\\id")]
    [InlineData("../escape")]
    [InlineData(".hidden")]
    [InlineData("trailing.")]
    [InlineData("pack id")]
    [InlineData("pack:id")]
    public void Refuses_anything_unsafe_or_ambiguous(string? candidate)
    {
        Assert.False(ContentId.IsValid(candidate));
        Assert.False(ContentId.TryCreate(candidate, out _));
    }

    [Fact]
    public void Refuses_an_identifier_past_the_limit()
        => Assert.False(ContentId.IsValid(new string('a', ContentId.MaxLength + 1)));

    [Fact]
    public void Create_throws_when_the_caller_skipped_validation()
        => Assert.Throws<ArgumentException>(() => ContentId.Create("Not Valid"));

    [Fact]
    public void Compares_by_value()
        => Assert.Equal(ContentId.Create("dnd5e"), ContentId.Create("dnd5e"));
}
