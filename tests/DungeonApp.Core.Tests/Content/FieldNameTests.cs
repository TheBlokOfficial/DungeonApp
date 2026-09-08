using System;
using DungeonApp.Core.Content;

namespace DungeonApp.Core.Tests.Content;

public sealed class FieldNameTests
{
    [Theory]
    [InlineData("kp")]
    [InlineData("kpZrodlo")]
    [InlineData("cechySzczegolne")]
    [InlineData("Field2")]
    public void Accepts_a_camel_case_identifier(string candidate)
        => Assert.True(FieldName.TryCreate(candidate, out _));

    /// <summary>
    /// Different rules than <see cref="ContentId"/> on purpose: a field name never becomes a file
    /// name, so mixed case is fine, but it does address a slot in data, so it must read as a plain
    /// identifier rather than something that could be confused with a bare number.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1abc")]
    [InlineData("field-name")]
    [InlineData("field.name")]
    [InlineData("field name")]
    [InlineData("field:name")]
    public void Refuses_anything_that_is_not_a_plain_identifier(string? candidate)
    {
        Assert.False(FieldName.IsValid(candidate));
        Assert.False(FieldName.TryCreate(candidate, out _));
    }

    [Fact]
    public void Refuses_a_name_past_the_limit()
        => Assert.False(FieldName.IsValid(new string('a', FieldName.MaxLength + 1)));

    [Fact]
    public void Create_throws_when_the_caller_skipped_validation()
        => Assert.Throws<ArgumentException>(() => FieldName.Create("1abc"));

    [Fact]
    public void Compares_by_value()
        => Assert.Equal(FieldName.Create("kpZrodlo"), FieldName.Create("kpZrodlo"));
}
