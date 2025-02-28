using System.Text;

namespace OneCore;

public interface ISerializer
{
    Encoding Encoding { get; }

    object? Deserialize(byte[]? data, Type objectType);

    byte[] Serialize(object? model, Type? objectType = null);
}