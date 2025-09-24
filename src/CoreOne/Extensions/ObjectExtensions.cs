namespace CoreOne.Extensions;

public static class ObjectExtensions
{
    public static bool IsNotNull([NotNullWhen(false)] this object? o) => !o.IsNull();

    public static bool IsNull([NotNullWhen(false)] this object? o) => (o == null) || (o == DBNull.Value);

    public static IDictionary<string, object> ToODictionary(this object? o, string separator = "-")
    {
        var data = new Data<string, object>(15);
        if (o != null)
        {
            var meta = MetaType.GetMetadatas(o.GetType());
            foreach (var kp in meta)
            {
                var value = kp.GetValue(o);
                if (value != null)
                    data.Set(kp.Name.Separate(separator), value);
            }
        }
        return data;
    }
}