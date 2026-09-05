using System.Collections.Generic;
using System.Linq;

namespace DungeonApp.Core.DataBlocks;

/// <summary>
/// Describes the BUILD of a data block's value - what shape a value must have to be accepted as
/// this data block's content. A shape checks values; it never holds one.
/// <para>
/// Kept as a small closed hierarchy today (primitives and flat objects only). Nesting and
/// references are meant to arrive later as new subtypes of this same record, which is why
/// <see cref="FieldShape.Type"/> is already typed as <see cref="DataBlockShape"/> rather than an
/// enum - the extension point exists before it is used.
/// </para>
/// </summary>
public abstract record DataBlockShape
{
    public abstract bool Matches(object? value);
}

/// <summary>
/// The scalar kinds a <see cref="PrimitiveShape"/> can require.
/// </summary>
public enum PrimitiveKind
{
    Integer,
    Fractional,
    Text,
    Boolean,
}

/// <summary>
/// A single scalar value.
/// <para>
/// <see cref="PrimitiveKind.Integer"/> and <see cref="PrimitiveKind.Fractional"/> replace the old,
/// single <c>Number</c> variant on purpose: a shape that only knew "this is a number" could not
/// tell a later persistence layer whether to reconstruct an <c>int</c> or a <c>double</c> after a
/// round trip through JSON, where both look alike once the value comes back as a floating-point
/// number. The shape now carries that decision itself instead of leaving it to be guessed from
/// whatever a parser happened to produce.
/// </para>
/// <para>
/// This is the one place that says which CLR type belongs to which kind:
/// <list type="bullet">
/// <item><description><see cref="PrimitiveKind.Integer"/>: <c>sbyte</c>, <c>byte</c>, <c>short</c>,
/// <c>ushort</c>, <c>int</c>, <c>uint</c>, <c>long</c>, <c>ulong</c>.</description></item>
/// <item><description><see cref="PrimitiveKind.Fractional"/>: <c>float</c>, <c>double</c>,
/// <c>decimal</c>.</description></item>
/// </list>
/// The two sets are disjoint - an integral value never matches a fractional field and a
/// floating-point or decimal value never matches an integer field, even though CLR numeric
/// conversions could losslessly promote most integers into any of the fractional types. Silent
/// promotion is exactly the kind of surprise this split exists to remove: a field declared
/// fractional says "this is meant to carry a fraction", and a value that happens not to have one
/// yet is still the caller's job to produce as a fractional CLR type.
/// </para>
/// </summary>
public sealed record PrimitiveShape(PrimitiveKind Kind) : DataBlockShape
{
    public override bool Matches(object? value) => Kind switch
    {
        PrimitiveKind.Integer => IsInteger(value),
        PrimitiveKind.Fractional => IsFractional(value),
        PrimitiveKind.Text => value is string,
        PrimitiveKind.Boolean => value is bool,
        _ => false,
    };

    private static bool IsInteger(object? value) => value
        is sbyte or byte or short or ushort or int or uint or long or ulong;

    private static bool IsFractional(object? value) => value
        is float or double or decimal;
}

/// <summary>One named field inside an <see cref="ObjectShape"/>.</summary>
public sealed record FieldShape(string Name, DataBlockShape Type);

/// <summary>
/// A flat record: a fixed set of named fields, each with its own shape.
/// <para>
/// A value matches only when it declares exactly these fields - nothing missing, nothing extra.
/// An undeclared field present in the data is rejected rather than ignored: a shape that let extra
/// data ride along silently would guarantee nothing about what is actually in the data block, which
/// defeats the point of having a shape at all.
/// </para>
/// </summary>
public sealed record ObjectShape(IReadOnlyList<FieldShape> Fields) : DataBlockShape
{
    public override bool Matches(object? value)
    {
        if (value is not IReadOnlyDictionary<string, object> data)
        {
            return false;
        }

        if (data.Count != Fields.Count)
        {
            return false;
        }

        return Fields.All(field =>
            data.TryGetValue(field.Name, out var fieldValue) && field.Type.Matches(fieldValue));
    }
}
