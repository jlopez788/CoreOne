using Newtonsoft.Json;
using System.Text;

namespace OneCore.Services;

public class NJsonSerializer(JsonSerializerSettings? settings = null) : ISerializer
{
    public static readonly NJsonSerializer Instance = new();
    public Encoding Encoding { get; set; } = Encoding.UTF8;
    public JsonSerializerSettings Settings { get; } = settings ?? new NewtonSettings();

    public IResult<T> Deserialize<T>(byte[]? data)
    {
        IResult<T> result = new Result<T>(ResultType.Fail, "Invalid data");
        try
        {
            if (data?.Length > 0)
            {
                var content = JsonConvert.DeserializeObject<T>(Encoding.GetString(data), Settings);
                result = new Result<T>(content, true);
            }
        }
        catch { }
        return result;
    }

    public object? Deserialize(byte[]? data, Type objectType)
    {
        if (data?.Length > 0)
        {
            var content = Encoding.GetString(data);
            return JsonConvert.DeserializeObject(content, objectType, Settings);
        }
        return null;
    }

    public byte[] Serialize(object? model, Type? objectType = null)
    {
        var content = JsonConvert.SerializeObject(model, objectType, Settings);
        return Encoding.GetBytes(content);
    }
}