namespace DungeonApp.Core.Content;

/// <summary>
/// The value an entry supplies for one of its template's fields. A closed hierarchy - the private
/// protected constructor means only the two variants declared in this file can ever exist - because
/// the set of scalar kinds is exactly <see cref="FieldType"/>, and a third kind of value appearing
/// without a matching third kind of field would be a silent way to smuggle a shape the catalog
/// never agreed to.
/// </summary>
public abstract record FieldValue
{
    private protected FieldValue()
    {
    }
}

/// <summary>A field value that is a piece of text, exactly as the pack wrote it.</summary>
public sealed record TextValue(string Text) : FieldValue;

/// <summary>
/// A field value that is a whole number. <c>long</c>, not <c>int</c>, for the same reason
/// <see cref="DataBlocks.PrimitiveKind.Integer"/> normalizes to <c>long</c>: it is the widest
/// integral type, so nothing a pack author writes can overflow it.
/// </summary>
public sealed record IntegerValue(long Value) : FieldValue;
