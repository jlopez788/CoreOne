namespace CoreOne.Http;

/// <summary>
/// Executes <see cref="HttpRequestMessage"/> requests and deserializes responses into CoreOne result types or models.
/// </summary>
public static class RestClientInvoker
{
    public static async Task SendAsync(
        HttpClient client,
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        using (request)
        {
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }
    }

    public static async Task<HttpResult> SendForHttpResultAsync(
        HttpClient client,
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using (request)
            {
                using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
                return new HttpResult(response) {
                    Uri = response.RequestMessage?.RequestUri?.ToString()
                };
            }
        }
        catch (Exception ex)
        {
            return HttpResult.FromException(ex);
        }
    }

    public static async Task<HttpResult<TModel>> SendForHttpResultAsync<TModel>(
        HttpClient client,
        HttpRequestMessage request,
        ISerializer serializer,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using (request)
            {
                using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
                return await HttpResult.FromResponseAsync<TModel>(response, serializer, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            return HttpResult.FromException<TModel>(ex);
        }
    }

    public static async Task<HttpResult<TModel, TError>> SendForHttpResultAsync<TModel, TError>(
        HttpClient client,
        HttpRequestMessage request,
        ISerializer serializer,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using (request)
            {
                using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
                return await HttpResult.FromResponseAsync<TModel, TError>(response, serializer, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            return HttpResult.FromException<TModel, TError>(ex);
        }
    }

    public static async Task<IResult> SendForIResultAsync(
        HttpClient client,
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        return await SendForHttpResultAsync(client, request, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<IResult<TModel>> SendForIResultAsync<TModel>(
        HttpClient client,
        HttpRequestMessage request,
        ISerializer serializer,
        CancellationToken cancellationToken = default)
    {
        return await SendForHttpResultAsync<TModel>(client, request, serializer, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<TModel?> SendForModelAsync<TModel>(
        HttpClient client,
        HttpRequestMessage request,
        ISerializer serializer,
        CancellationToken cancellationToken = default)
    {
        using (request)
        {
            if (typeof(TModel) == typeof(HttpResponseMessage))
            {
                var rawResponse = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
                return (TModel?)(object?)rawResponse;
            }

            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            if (typeof(TModel) == Types.String)
            {
#if NET9_0_OR_GREATER
                var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
                return (TModel?)(object?)content;
            }

            if (typeof(TModel) == Types.Bytes)
            {
#if NET9_0_OR_GREATER
                var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
#else
                var bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
#endif
                return (TModel?)(object?)bytes;
            }

#if NET9_0_OR_GREATER
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#else
            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
            var deserialized = await serializer.DeserializeAsync(stream, typeof(TModel), cancellationToken).ConfigureAwait(false);
            if (!deserialized.Success)
                throw new InvalidOperationException(deserialized.Message ?? $"Failed to deserialize response as {typeof(TModel).Name}.");

            return deserialized.Model is TModel typed ? typed : default;
        }
    }

    public static async Task<Result> SendForResultAsync(
        HttpClient client,
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        var httpResult = await SendForHttpResultAsync(client, request, cancellationToken).ConfigureAwait(false);
        return new Result(httpResult.ResultType, httpResult.Message);
    }

    public static async Task<Result<TModel>> SendForResultAsync<TModel>(
        HttpClient client,
        HttpRequestMessage request,
        ISerializer serializer,
        CancellationToken cancellationToken = default)
    {
        var httpResult = await SendForHttpResultAsync<TModel>(client, request, serializer, cancellationToken).ConfigureAwait(false);
        return httpResult.Success
            ? new Result<TModel>(httpResult.Model)
            : new Result<TModel>(httpResult.Model, httpResult.Message, httpResult.ResultType);
    }
}
