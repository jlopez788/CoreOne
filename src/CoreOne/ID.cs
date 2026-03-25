using CoreOne.Converters;

namespace CoreOne;

[NJsonConverter(typeof(IDConverter.NewtonsoftConverter))]
[SJsonConverter(typeof(IDConverter.SystemJsonConverter))]
public class ID : ICoreId<ID>, IEquatable<ID>
{
    public static ID Empty { get; } = new(Guid.Empty);

    public Guid Value { get; }

    public ID(Guid id) => Value = id;

    public ID() => Value = GetSequentialGuid();

    private static Guid GetSequentialGuid()
    {
#if NET9_0_OR_GREATER
        return Guid.CreateVersion7();
#else
        byte[] uid = Guid.NewGuid().ToByteArray();
        byte[] seq = BitConverter.GetBytes(DateTime.UtcNow.Ticks);
        Array.Reverse(seq);
        // The first 8 bytes are sequential, it minimizes index fragmentation
        Array.Copy(seq, uid, seq.Length);
        return new Guid(uid);
#endif
    }

    /// <summary>
    /// Creates sequential ID
    /// </summary>
    /// <returns></returns>
    public static ID Create() => new(GetSequentialGuid());

    /// <summary>
    /// Creates sequential ID
    /// </summary>
    /// <param name="id">Optional guid id</param>
    /// <returns></returns>
    public static ID CreateFromGuid(Guid? id) => id.HasValue ? new ID(id.Value) : Create();

    public static bool operator !=(ID? left, ID? right) => left?.Value != right?.Value;

    public static bool operator ==(ID? left, ID? right) => left?.Value == right?.Value;

    public static implicit operator ID(Guid key) => new(key);

    public static bool TryParse(string? value, [NotNullWhen(true)] out ID? id)
    {
        id = null;
        if (!string.IsNullOrEmpty(value))
        {
            if (Guid.TryParse(value, out var guid))
                id = new ID(guid);
            else if (value!.Length == 22)
            {
                try
                {
                    var next = value.Replace("_", "/").Replace("-", "+") + "==";
                    var data = Convert.FromBase64String(next);
                    if (data.Length == 16)
                        id = new ID(new Guid(data));
                }
                catch { }
            }
        }

        return id is not null;
    }

    public Guid AsGuid() => Value;

    public bool Equals(ID? other) => Value == other?.Value;

    public override bool Equals(object? obj) => obj is ID id && Value == id.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}