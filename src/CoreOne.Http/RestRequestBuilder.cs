namespace CoreOne.Http;

/// <summary>
/// Fluent builder for constructing <see cref="HttpRequestMessage"/> instances from declarative REST interface metadata.
/// </summary>
public class RestRequestBuilder
{
    private readonly List<(string Key, string Value)> Headers = [];
    private readonly HttpMethod Method;
    private readonly List<string> QuerySegments = [];
    private string? AuthScheme;
    private string? AuthToken;
    private HttpContent? BodyContent;
    private MultipartFormDataContent? MultipartContent;
    private string RoutePath;

    public RestRequestBuilder(string httpMethod, [StringSyntax("Route")] string? basePath = null, [StringSyntax("Route")] string? relativePath = null)
    {
        Method = ParseHttpMethod(httpMethod);
        RoutePath = CombinePaths(basePath, relativePath);
    }

    public RestRequestBuilder AddHeader(string name, object? value)
    {
        if (!string.IsNullOrWhiteSpace(name) && value is not null)
        {
            var formatted = FormatValue(value, null);
            if (!string.IsNullOrEmpty(formatted))
                Headers.Add((name.Trim(), formatted));
        }
        return this;
    }

    public RestRequestBuilder AddHeaders(params string[]? headers)
    {
        if (headers is null || headers.Length == 0)
            return this;

        foreach (var header in headers)
        {
            if (string.IsNullOrWhiteSpace(header))
                continue;

            var separatorIndex = header.IndexOf(':');
            if (separatorIndex <= 0)
                continue;

            var name = header.Substring(0, separatorIndex).Trim();
            var value = header.Substring(separatorIndex + 1).Trim();
            if (name.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
            {
                var spaceIdx = value.IndexOf(' ');
                if (spaceIdx > 0)
                {
                    AuthScheme = value.Substring(0, spaceIdx).Trim();
                    AuthToken = value.Substring(spaceIdx + 1).Trim();
                    continue;
                }
            }
            Headers.Add((name, value));
        }
        return this;
    }

    public RestRequestBuilder AddMultipartItem(string name, object? value, ISerializer serializer)
    {
        if (value is null)
            return this;

        MultipartContent ??= new MultipartFormDataContent("----CoreOneMultipartBoundary");

        switch (value)
        {
            case MultipartItem part:
                MultipartContent.Add(part.ToHttpContent(), part.Name ?? name, part.FileName);
                break;

            case IEnumerable<MultipartItem> parts:
                foreach (var p in parts)
                    MultipartContent.Add(p.ToHttpContent(), p.Name ?? name, p.FileName);
                break;

            case HttpContent httpContent:
                MultipartContent.Add(httpContent, name);
                break;

            case Stream stream:
                MultipartContent.Add(new StreamContent(stream), name, name);
                break;

            case byte[] bytes:
                MultipartContent.Add(new ByteArrayContent(bytes), name, name);
                break;

            case FileInfo fileInfo:
                MultipartContent.Add(new StreamContent(fileInfo.OpenRead()), name, fileInfo.Name);
                break;

            case string str:
                MultipartContent.Add(new StringContent(str, Encoding.UTF8), name);
                break;

            default:
                if (IsSimpleValueType(value.GetType()))
                {
                    MultipartContent.Add(new StringContent(FormatValue(value, null), Encoding.UTF8), name);
                }
                else
                {
                    var serialized = serializer.ToHttpContent(value);
                    if (serialized is not null)
                        MultipartContent.Add(serialized, name);
                }
                break;
        }

        return this;
    }

    public RestRequestBuilder AddQueryParam(string name, object? value, string? format = null)
    {
        if (value is null)
            return this;

        if (value is ODataBuilder odata)
        {
            var odataQuery = odata.ToString().TrimStart('?');
            if (!string.IsNullOrWhiteSpace(odataQuery))
                QuerySegments.Add(odataQuery);
            return this;
        }

        if (value is string str)
        {
            QuerySegments.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(str)}");
            return this;
        }

        if (value is IEnumerable<KeyValuePair<string, string?>> kvps)
        {
            foreach (var kvp in kvps)
            {
                if (kvp.Value is not null)
                    QuerySegments.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}");
            }
            return this;
        }

        if (value is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Key is not null && entry.Value is not null)
                    AddSingleQueryParam(entry.Key.ToString()!, entry.Value, format);
            }
            return this;
        }

        if (value is IEnumerable enumerable and not byte[])
        {
            foreach (var item in enumerable)
            {
                if (item is not null)
                    AddSingleQueryParam(name, item, format);
            }
            return this;
        }

        var valueType = value.GetType();
        if (IsSimpleValueType(valueType))
        {
            AddSingleQueryParam(name, value, format);
            return this;
        }

        var metadatas = MetaType.GetMetadatas(valueType);
        foreach (var meta in metadatas)
        {
            if (IsIgnoredMember(meta))
                continue;

            var propValue = meta.GetValue(value);
            if (propValue is null)
                continue;

            var propName = meta.GetCustomAttribute<AliasAsAttribute>(true)?.Name ?? meta.Name;
            AddQueryParam(propName, propValue, null);
        }

        return this;
    }

    public RestRequestBuilder AddRouteParam(string name, object? value, string? format = null)
    {
        var formatted = value is null ? string.Empty : FormatValue(value, format);
        var escaped = Uri.EscapeDataString(formatted);

        var catchAllToken = $"{{*{name}}}";
        if (RoutePath.IndexOf(catchAllToken, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            var catchAllEscaped = string.Join("/", formatted.Split('/').Select(Uri.EscapeDataString));
            RoutePath = ReplaceTokenCaseInsensitive(RoutePath, catchAllToken, catchAllEscaped);
        }

        var standardToken = $"{{{name}}}";
        if (RoutePath.IndexOf(standardToken, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            RoutePath = ReplaceTokenCaseInsensitive(RoutePath, standardToken, escaped);
        }

        return this;
    }

    public RestRequestBuilder AsMultipart(string boundary = "----CoreOneMultipartBoundary")
    {
        MultipartContent ??= new MultipartFormDataContent(string.IsNullOrWhiteSpace(boundary) ? "----CoreOneMultipartBoundary" : boundary);
        return this;
    }

    public HttpRequestMessage Build()
    {
        var uriString = BuildUriString();
        var request = new HttpRequestMessage(Method, uriString);

        var content = MultipartContent ?? BodyContent;
        if (content is not null)
            request.Content = content;

        foreach (var (key, value) in Headers)
        {
            var addedToRequest = false;
            try
            {
                if (request.Headers.Contains(key))
                    request.Headers.Remove(key);

                addedToRequest = request.Headers.TryAddWithoutValidation(key, value);
            }
            catch (InvalidOperationException)
            {
                // Content-scoped header (e.g. Content-Type, Content-Language); route to content headers below
            }

            if (!addedToRequest && request.Content is not null)
            {
                if (request.Content.Headers.Contains(key))
                    request.Content.Headers.Remove(key);
                request.Content.Headers.TryAddWithoutValidation(key, value);
            }
        }

        if (!string.IsNullOrWhiteSpace(AuthScheme) && !string.IsNullOrWhiteSpace(AuthToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(AuthScheme, AuthToken);
        }

        return request;
    }

    public RestRequestBuilder WithAuthorization(string scheme, object? token)
    {
        var tokenStr = token?.ToString();
        if (!string.IsNullOrWhiteSpace(tokenStr))
        {
            AuthScheme = string.IsNullOrWhiteSpace(scheme) ? "Bearer" : scheme;
            AuthToken = tokenStr!.Trim();
        }
        return this;
    }

    public RestRequestBuilder WithBody(object? body, ISerializer serializer, BodySerializationMethod serializationMethod = BodySerializationMethod.Serialized)
    {
        if (body is null)
            return this;

        if (body is HttpContent rawContent)
        {
            BodyContent = rawContent;
            return this;
        }

        if (body is Stream stream)
        {
            BodyContent = new StreamContent(stream);
            return this;
        }

        if (serializationMethod == BodySerializationMethod.UrlEncoded)
        {
            BodyContent = new FormUrlEncodedContent(ExtractFormFields(body));
            return this;
        }

        BodyContent = serializer.ToHttpContent(body);
        return this;
    }

    private static string CombinePaths(string? basePath, string? relativePath)
    {
        var rel = relativePath?.Trim() ?? string.Empty;
        if (rel.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            rel.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return rel;
        }

        var @base = basePath?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(@base))
            return rel;

        if (string.IsNullOrEmpty(rel))
            return @base;

        return $"{@base.TrimEnd('/')}/{rel.TrimStart('/')}";
    }

    private static List<KeyValuePair<string, string>> ExtractFormFields(object body)
    {
        var pairs = new List<KeyValuePair<string, string>>();

        if (body is IEnumerable<KeyValuePair<string, string>> stringPairs)
        {
            pairs.AddRange(stringPairs);
            return pairs;
        }

        if (body is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Key is not null && entry.Value is not null)
                    pairs.Add(new KeyValuePair<string, string>(entry.Key.ToString()!, FormatValue(entry.Value, null)));
            }
            return pairs;
        }

        var metadatas = MetaType.GetMetadatas(body.GetType());
        foreach (var meta in metadatas)
        {
            if (IsIgnoredMember(meta))
                continue;

            var val = meta.GetValue(body);
            if (val is null)
                continue;

            var key = meta.GetCustomAttribute<AliasAsAttribute>(true)?.Name ?? meta.Name;
            if (val is IEnumerable enumerable and not string and not byte[])
            {
                foreach (var item in enumerable)
                {
                    if (item is not null)
                        pairs.Add(new KeyValuePair<string, string>(key, FormatValue(item, null)));
                }
            }
            else
            {
                pairs.Add(new KeyValuePair<string, string>(key, FormatValue(val, null)));
            }
        }

        return pairs;
    }

    private static string FormatValue(object value, string? format)
    {
        if (value is string str)
            return str;

        if (value is ILookupType lookup)
            return lookup.Code;

        if (value is bool b)
            return b ? "true" : "false";

        if (value is Enum e)
            return e.ToString();

        if (!string.IsNullOrEmpty(format) && value is IFormattable formattableWithFormat)
            return formattableWithFormat.ToString(format, CultureInfo.InvariantCulture);

        if (value is DateTime dt)
            return dt.ToString("O", CultureInfo.InvariantCulture);

        if (value is DateTimeOffset dto)
            return dto.ToString("O", CultureInfo.InvariantCulture);

        if (value is IFormattable formattable)
            return formattable.ToString(null, CultureInfo.InvariantCulture);

        return value.ToString() ?? string.Empty;
    }

    private static bool IsIgnoredMember(Metadata meta) =>
        !meta.CanRead ||
        meta.IsStatic ||
        meta.GetCustomAttribute<NJsonIgnore>(true) is not null ||
        meta.GetCustomAttribute<SJsonIgnore>(true) is not null;

    private static bool IsSimpleValueType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (underlying.IsPrimitive ||
            underlying.IsEnum ||
            underlying == Types.String ||
            underlying == typeof(decimal) ||
            underlying == Types.Guid ||
            underlying == Types.DateTime ||
            underlying == typeof(DateTimeOffset) ||
            underlying == typeof(TimeSpan) ||
            underlying == typeof(Uri) ||
            Types.DotNetTypes.Contains(underlying) ||
            typeof(ILookupType).IsAssignableFrom(underlying))
        {
            return true;
        }

        if (underlying.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICoreId<,>)))
            return true;

        if (underlying.GetCustomAttribute<StronglyTypedIdAttribute>(true) is not null)
            return true;

        return false;
    }

    private static HttpMethod ParseHttpMethod(string method) => method?.ToUpperInvariant() switch {
        "GET" => HttpMethod.Get,
        "POST" => HttpMethod.Post,
        "PUT" => HttpMethod.Put,
        "DELETE" => HttpMethod.Delete,
        "HEAD" => HttpMethod.Head,
        "OPTIONS" => HttpMethod.Options,
        "PATCH" => new HttpMethod("PATCH"),
        "QUERY" => new HttpMethod("QUERY"),
        _ => new HttpMethod(string.IsNullOrWhiteSpace(method) ? "GET" : method)
    };

    private static string ReplaceTokenCaseInsensitive(string input, string token, string replacement)
    {
        var index = input.IndexOf(token, StringComparison.OrdinalIgnoreCase);
        while (index >= 0)
        {
            input = input.Substring(0, index) + replacement + input.Substring(index + token.Length);
            index = input.IndexOf(token, index + replacement.Length, StringComparison.OrdinalIgnoreCase);
        }
        return input;
    }

    private void AddSingleQueryParam(string name, object value, string? format)
    {
        var formatted = FormatValue(value, format);
        QuerySegments.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(formatted)}");
    }

    private string BuildUriString()
    {
        if (QuerySegments.Count == 0)
            return RoutePath;

        var query = string.Join("&", QuerySegments);
        var separator = RoutePath.Contains("?") ? "&" : "?";
        return $"{RoutePath}{separator}{query}";
    }
}
