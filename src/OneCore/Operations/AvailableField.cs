using System.Diagnostics;

namespace CoreOne.Operations;

[DebuggerDisplay("Available: {Field}")]
public class AvailableField : IOperation
{
    public string Field { get; }
    public string? Format { get; }
    public bool IsNullable { get; set; }
    public Type Type { get; }

    public AvailableField(Type type, string field, string? format = null)
    {
        Field = field;
        Format = format;
        Type = type;
        if (type is not null)
        {
            var check = Nullable.GetUnderlyingType(type);
            if (check is not null)
            {
                Type = check;
                IsNullable = true;
            }
        }
    }

    public override bool Equals(object? obj) => obj is AvailableField p && Field.Matches(p.Field) && IsNullable == p.IsNullable;

    public override int GetHashCode() => (Field, Format, IsNullable).GetHashCode();

    public override string ToString() => Field;
}
