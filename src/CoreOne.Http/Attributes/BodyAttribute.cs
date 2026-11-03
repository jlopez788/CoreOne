namespace CoreOne.Http.Attributes;

/// <summary>
/// Binds a method parameter as the HTTP request body.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public class BodyAttribute(BodySerializationMethod serializationMethod = BodySerializationMethod.Serialized, string? serializerKey = null) : Attribute
{
    public BodySerializationMethod SerializationMethod { get; } = serializationMethod;
    public string? SerializerKey { get; } = serializerKey;
}
