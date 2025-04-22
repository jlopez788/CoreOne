using Newtonsoft.Json;
using System.Text.Json;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace CoreOne.Converters;

public class FileSizeConverter
{
    public class NewtonsoftConverter : JsonConverter
    {
        private static readonly Type Type = typeof(FileSize);

        public override bool CanConvert(Type objectType) => objectType == Type;

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var value = serializer.Deserialize(reader) as long?;
            return value.HasValue ? new FileSize(value.Value) : new FileSize(0);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value is FileSize size ? size.Size : 0);
        }
    }

    public class SystemJsonConverter : System.Text.Json.Serialization.JsonConverter<FileSize>
    {
        public override FileSize Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetInt64();
            return new FileSize(value);
        }

        public override void Write(Utf8JsonWriter writer, FileSize value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value.Size);
        }
    }
}