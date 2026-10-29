using CoreOne.Lookups;
using Newtonsoft.Json;
using System.Text.Json;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace CoreOne.Converters;

public class LookupTypeConverter<T> where T : ILookupType<T>
{
    public class NewtonsoftConverter(T defaultCode) : JsonConverter
    {
        private static readonly Type Type = typeof(T);

        public override bool CanConvert(Type objectType) => objectType == Type;

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var value = serializer.Deserialize(reader)?.ToString();
            return value is not null ? FindType(value) : defaultCode;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value is T type ? type.Code : defaultCode.Code);
        }
    }

    public class SystemJsonConverter(T defaultCode) : System.Text.Json.Serialization.JsonConverter<T>
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            return FindType(value) ?? defaultCode;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Code);
        }
    }

    private static T? FindType(object? value)
    {
#if NET9_0_OR_GREATER
        return T.FindType(value);
#else
        var method = typeof(T).GetMethod(nameof(LookupType<>.FindType), BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        return method?.Invoke(null, [value]) is T result ? result : default;
#endif
    }
}
