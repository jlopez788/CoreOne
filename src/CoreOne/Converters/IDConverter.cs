using Newtonsoft.Json;
using System.Text.Json;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace CoreOne.Converters;

public class IDConverter
{
    public class NewtonsoftConverter : JsonConverter
    {
        private static readonly Type Type = typeof(ID);

        public override bool CanConvert(Type objectType) => objectType == Type;

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var value = serializer.Deserialize(reader)?.ToString();
            return ID.TryParse(value, out var id) ? id : ID.Empty;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value is ID id ? id.Value : Guid.Empty);
        }
    }

    public class SystemJsonConverter : System.Text.Json.Serialization.JsonConverter<ID>
    {
        public override ID Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetGuid();
            return new ID(value);
        }

        public override void Write(Utf8JsonWriter writer, ID value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Value);
        }
    }
}