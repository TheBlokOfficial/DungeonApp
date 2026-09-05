using System.Collections.Generic;
using DungeonApp.Core.DataBlocks;

namespace DungeonApp.Core.Tests.DataBlocks;

public sealed class DataBlockShapeTests
{
    private static readonly ObjectShape Shape = new(
    [
        new FieldShape("name", new PrimitiveShape(PrimitiveKind.Text)),
        new FieldShape("level", new PrimitiveShape(PrimitiveKind.Integer)),
        new FieldShape("active", new PrimitiveShape(PrimitiveKind.Boolean)),
    ]);

    [Fact]
    public void Matches_a_complete_value_with_correctly_typed_fields()
        => Assert.True(Shape.Matches(new Dictionary<string, object>
        {
            ["name"] = "Aria",
            ["level"] = 3,
            ["active"] = true,
        }));

    [Fact]
    public void Refuses_a_value_missing_a_declared_field()
        => Assert.False(Shape.Matches(new Dictionary<string, object>
        {
            ["name"] = "Aria",
            ["level"] = 3,
        }));

    [Fact]
    public void Refuses_a_value_whose_field_has_the_wrong_type()
        => Assert.False(Shape.Matches(new Dictionary<string, object>
        {
            ["name"] = "Aria",
            ["level"] = "three",
            ["active"] = true,
        }));

    /// <summary>
    /// An undeclared field is rejected rather than tolerated: a shape that lets extra data ride
    /// along silently would guarantee nothing about the data block's actual content.
    /// </summary>
    [Fact]
    public void Refuses_a_value_with_an_undeclared_field()
        => Assert.False(Shape.Matches(new Dictionary<string, object>
        {
            ["name"] = "Aria",
            ["level"] = 3,
            ["active"] = true,
            ["extra"] = "unexpected",
        }));

    [Fact]
    public void Refuses_a_value_that_is_not_a_dictionary_at_all()
        => Assert.False(Shape.Matches("not a dictionary"));

    [Theory]
    [InlineData((sbyte)1)]
    [InlineData((byte)1)]
    [InlineData((short)1)]
    [InlineData((ushort)1)]
    [InlineData(1)]
    [InlineData(1u)]
    [InlineData(1L)]
    [InlineData(1ul)]
    public void Integer_primitive_accepts_the_built_in_integral_types(object candidate)
        => Assert.True(new PrimitiveShape(PrimitiveKind.Integer).Matches(candidate));

    [Theory]
    [InlineData(1f)]
    [InlineData(1d)]
    public void Integer_primitive_refuses_floating_point_types(object candidate)
        => Assert.False(new PrimitiveShape(PrimitiveKind.Integer).Matches(candidate));

    [Fact]
    public void Integer_primitive_refuses_decimal()
        => Assert.False(new PrimitiveShape(PrimitiveKind.Integer).Matches(1m));

    [Fact]
    public void Integer_primitive_refuses_text()
        => Assert.False(new PrimitiveShape(PrimitiveKind.Integer).Matches("1"));

    [Theory]
    [InlineData(1f)]
    [InlineData(1d)]
    public void Fractional_primitive_accepts_the_built_in_floating_point_types(object candidate)
        => Assert.True(new PrimitiveShape(PrimitiveKind.Fractional).Matches(candidate));

    [Fact]
    public void Fractional_primitive_accepts_decimal()
        => Assert.True(new PrimitiveShape(PrimitiveKind.Fractional).Matches(1m));

    /// <summary>
    /// A whole number is mathematically a valid fraction, but this shape does not promote it: a
    /// field declared fractional is meant to carry values coming from fractional CLR code, and
    /// silently accepting an integer here would let a mismatch between the declared shape and the
    /// producing code hide until it resurfaces as a surprise elsewhere.
    /// </summary>
    [Fact]
    public void Fractional_primitive_refuses_an_integer()
        => Assert.False(new PrimitiveShape(PrimitiveKind.Fractional).Matches(1));

    [Fact]
    public void Fractional_primitive_refuses_text()
        => Assert.False(new PrimitiveShape(PrimitiveKind.Fractional).Matches("1"));

    [Fact]
    public void Text_primitive_refuses_a_number()
        => Assert.False(new PrimitiveShape(PrimitiveKind.Text).Matches(1));

    [Fact]
    public void Boolean_primitive_refuses_a_number()
        => Assert.False(new PrimitiveShape(PrimitiveKind.Boolean).Matches(1));
}
