using Microsoft.Extensions.DependencyInjection;

namespace CoreOne.Http.Extensions;

public static class SerializerHttpExtensions
{
    public static ISerializer ResolveSerializer(
        this IServiceProvider? services,
        string? key = null,
        Type? serializerType = null,
        ISerializer? fallback = null)
    {
        if (serializerType is not null)
        {
            if (services?.Resolve(serializerType) is ISerializer typedFromServices)
                return typedFromServices;

            if (Activator.CreateInstance(serializerType) is ISerializer created)
                return created;
        }

        if (!string.IsNullOrWhiteSpace(key) && services is not null)
        {
            var keyed = services.GetKeyedService<ISerializer>(key);
            if (keyed is not null)
                return keyed;
        }

        if (fallback is not null)
            return fallback;

        if (services is not null)
        {
            var defaultSerializer = services.GetService<ISerializer>() ?? services.GetKeyedService<ISerializer>("json");
            if (defaultSerializer is not null)
                return defaultSerializer;
        }

        return NJsonService.Instance;
    }

    [return: NotNullIfNotNull(nameof(model))]
    public static HttpContent? ToHttpContent(this ISerializer serializer, object? model, Type? objectType = null)
    {
        if (model is null)
            return null;

        if (model is HttpContent existingContent)
            return existingContent;

        var bytes = serializer.Serialize(model, objectType ?? model.GetType());
        var content = new ByteArrayContent(bytes);
        var contentType = string.IsNullOrWhiteSpace(serializer.ContentType) ? "application/json" : serializer.ContentType;
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType) {
            CharSet = (serializer.Encoding ?? Encoding.UTF8).WebName
        };
        return content;
    }
}
