using System.Text;

namespace CoreOne;

public interface ISerializer
{
    Encoding Encoding { get; }

    IResult<object> Deserialize(byte[]? data, Type objectType);

    Task<IResult<object>> DeserializeAsync(Stream stream, Type objectType, CancellationToken cancellationToken = default);

    byte[] Serialize(object? model, Type? objectType = null);
}