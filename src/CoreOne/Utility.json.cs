using Newtonsoft.Json;
using System.Text;

namespace CoreOne;

public partial class Utility
{
    public static JsonSerializerSettings JsonSettings { get; private set; } = default!;

    public static object? Deserialize(Type type, string? content)
    {
        InitializeSettings();
        return !string.IsNullOrEmpty(content) ? type == Types.String ? content : JsonConvert.DeserializeObject(content!, type, JsonSettings) : null;
    }

    public static T? DeserializeObject<T>(string? jsonContent)
    {
        InitializeSettings();
        return !string.IsNullOrEmpty(jsonContent) ? jsonContent is T value ? value : JsonConvert.DeserializeObject<T>(jsonContent!, JsonSettings) : default;
    }

    public static IResult<T> DeserializeObject<T>(Stream stream)
    {
        InitializeSettings();
        try
        {  
            using var txtReader = new StreamReader(stream);
            using var reader = new JsonTextReader(txtReader);
            var serializer = JsonSerializer.Create(JsonSettings);
            var model = serializer.Deserialize<T>(reader);
            return new Result<T>(model);
        }
        catch (Exception ex)
        {
            return Result.FromException<T>(ex);
        }
    }

    public static void InitializeSettings()
    {
        if (JsonSettings == null)
        {// Initialize
            UseSettings(null);
        }
    }

    public static string Serialize<T>(T? entity, bool prettyPrint = false)
    {
        InitializeSettings();
        return entity is not null ? JsonConvert.SerializeObject(entity, typeof(T), prettyPrint ? Formatting.Indented : Formatting.None, JsonSettings) : string.Empty;
    }

    public static IResult SerializeToStream<T>(T entity, Stream stream, Encoding? encoding = null, bool prettyPrint = false)
    {
        var result = Result.Ok;
        InitializeSettings();
        try
        {
            encoding ??= Encoding.UTF8;

            using var txtWriter = new StreamWriter(stream, encoding);
            using var writer = new JsonTextWriter(txtWriter);
            var serializer = JsonSerializer.Create(JsonSettings);
            serializer.Formatting = prettyPrint ? Formatting.Indented : Formatting.None;
            serializer.Serialize(writer, entity);
        }
        catch (Exception ex) { result = Result.FromException(ex); }
        return result;
    }

    public static void UseSettings(JsonSerializerSettings? settings = null)
    {
        JsonSettings = settings ?? NewtonSettings.Default;
    }
}