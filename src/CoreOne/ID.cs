namespace CoreOne;

public class ID : IEquatable<ID>
{
    public static readonly ID Empty = new(Guid.Empty);
    internal readonly Guid Id;

    public ID(Guid id) => Id = id;

    public ID() => Id = Create();

    public static ID Create()
    {
        byte[] uid = Guid.NewGuid().ToByteArray();
        byte[] seq = BitConverter.GetBytes(DateTime.UtcNow.Ticks);
        Array.Reverse(seq);
        // The first 8 bytes are sequential, it minimizes index fragmentation
        Array.Copy(seq, uid, seq.Length);
        return new ID(new Guid(uid));
    }

    public static ID CreateV7() =>
#if NET9_0_OR_GREATER
        new(Guid.CreateVersion7());
#else
        Create();
#endif

    public static implicit operator Guid(ID id) => id.Id;

    public static bool operator !=(ID? left, ID? right) => left?.Id != right?.Id;

    public static bool operator ==(ID? left, ID? right) => left?.Id == right?.Id;

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

    public Guid AsGuid() => Id;

    public bool Equals(ID? other) => Id == other?.Id;

    public override bool Equals(object? obj) => obj is ID id && Id == id.Id;

    public override int GetHashCode() => Id.GetHashCode();

    public string ToShortId() => Id.ToShortId();

    public string ToSlugUrl() => Id.ToSlugUrl();

    public override string ToString() => Id.ToString();
}