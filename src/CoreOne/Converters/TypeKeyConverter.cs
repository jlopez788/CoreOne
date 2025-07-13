using Newtonsoft.Json;
using System.Text.Json;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace CoreOne.Converters;

public sealed class TypeKeyConverter
{
    public class NewtonsoftConverter : JsonConverter
    {
        private static readonly Type Type = typeof(TypeKey);

        public override bool CanConvert(Type objectType) => objectType == Type;

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var value = serializer.Deserialize<string>(reader)?.ToString();
            return string.IsNullOrEmpty(value) ? TypeKey.Empty : TypeKey.FindType(value);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value is TypeKey key ? key.Name : null);
        }
    }

    public class SystemJsonConverter : System.Text.Json.Serialization.JsonConverter<TypeKey>
    {
        public override TypeKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            return string.IsNullOrEmpty(value) ? TypeKey.Empty : TypeKey.FindType(value);
        }

        public override void Write(Utf8JsonWriter writer, TypeKey value, JsonSerializerOptions options)
        {
            if (value.Equals(TypeKey.Empty))
                writer.WriteNullValue();
            else
                writer.WriteStringValue(value.Name);
        }
    }
}