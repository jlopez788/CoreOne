using Newtonsoft.Json;
using System.Text;

namespace OneCore.Services;

public class NJsonService(JsonSerializerSettings? settings) : ISerializer
{
    public static readonly NJsonService Instance = new(null);
    public Encoding Encoding { get; set; } = Encoding.UTF8;
    public JsonSerializerSettings Settings { get; } = settings ?? new NewtonSettings();

    public IResult<object> Deserialize(byte[]? data, Type objectType)
    {
        try
        {
            if (data?.Length > 0)
            {
                var content = Encoding.GetString(data);
                return new Result<object>(JsonConvert.DeserializeObject(content, objectType, Settings));
            }
            return new Result<object>(null);
        }
        catch (Exception ex)
        {
            return Result.FromException<object>(ex);
        }
    }

    public async Task<IResult<object>> DeserializeAsync(Stream stream, Type objectType, CancellationToken cancellationToken = default)
    {
        try
        {
            using var reader = new StreamReader(stream, Encoding);
            using var json = new JsonTextReader(reader);
            var serializer = JsonSerializer.Create(Settings);
            return await Task.Factory.StartNew(() => new Result<object>(serializer.Deserialize(json, objectType)), cancellationToken);
        }
        catch (Exception ex)
        {
            return Result.FromException<object>(ex);
        }
    }

    public byte[] Serialize(object? model, Type? objectType = null)
    {
        var content = JsonConvert.SerializeObject(model, objectType, Settings);
        return Encoding.GetBytes(content);
    }
}