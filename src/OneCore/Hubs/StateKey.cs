namespace CoreOne.Hubs;

internal record StateKey(Type Type, string? Name = null)
{
    public static StateKey Create<T>(string? name = null) => new(typeof(T), name);
}