namespace CoreOne.Http.Attributes;

/// <summary>
/// Specifies the <see cref="ISerializer"/> key or implementation type to use for request/response serialization on an interface or method.
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false)]
public class SerializerAttribute : Attribute
{
    public string? Key { get; }
    public Type? SerializerType { get; }

    public SerializerAttribute(string key) => Key = key;

    public SerializerAttribute(Type serializerType) => SerializerType = serializerType;
}

/// <summary>
/// Specifies the <see cref="ISerializer"/> implementation type to use for request/response serialization on an interface or method.
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false)]
public class SerializerAttribute<TSerializer>() : SerializerAttribute(typeof(TSerializer)) where TSerializer : ISerializer;
