using System.Text;

namespace CoreOne.Extensions;

public static class HttpClientExtensions
{
    /// <summary>
    /// Sends a DELETE request to specified Uri
    /// </summary>
    /// <param name="client"></param>
    /// <param name="requestUri"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<HttpResult> DeleteXAsync(this HttpClient client, string requestUri, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await client.DeleteAsync(requestUri, cancellationToken).ConfigureAwait(false);
            return new HttpResult(response);
        }
        catch (Exception ex) { return HttpResult.FromException(ex); }
    }

    /// <summary>
    /// Sends a GET request to the specified Uri
    /// </summary>
    /// <param name="client"></param>
    /// <param name="requestUri"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<HttpResult<string>> GetStringXAsync(this HttpClient client, string requestUri, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
#if NET9_0_OR_GREATER
            return await HttpResult.FromResponse(response, p => p.ReadAsStringAsync(cancellationToken));
#else
            return await HttpResult.FromResponse(response, p => p.ReadAsStringAsync());
#endif
        }
        catch (Exception ex) { return HttpResult.FromException<string>(ex); }
    }

    /// <summary>
    /// Sends a GET request to the specified Uri
    /// </summary>
    /// <param name="client"></param>
    /// <param name="requestUri"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<HttpResult<T>> GetXAsync<T>(this HttpClient client, string requestUri, CancellationToken cancellationToken = default)
    {
        return await client.GetStringXAsync(requestUri, cancellationToken)
            .SelectFromJsonContent<T>()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a POST request to the specified Uri with given model
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    /// <param name="client"></param>
    /// <param name="model"></param>
    /// <param name="requestUri"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static Task<HttpResult<TModel>> PostXAsync<TModel>(this HttpClient client, TModel? model, string requestUri, CancellationToken cancellationToken = default)
    {
        return PostXAsync<TModel, TModel>(client, model, requestUri, cancellationToken);
    }

    /// <summary>
    /// Sends a POST request to the specified Uri with given model
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="client"></param>
    /// <param name="model"></param>
    /// <param name="requestUri"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<HttpResult<TResult>> PostXAsync<TModel, TResult>(this HttpClient client, TModel? model, string requestUri, CancellationToken cancellationToken = default)
    {
        try
        {
            var content = model.ToStringContent();
            using var response = await client.PostAsync(requestUri, content, cancellationToken).ConfigureAwait(false);
#if NET9_0_OR_GREATER
            return await HttpResult.FromResponse(response, p => p.ReadAsStringAsync(cancellationToken))
                    .SelectFromJsonContent<TResult>();
#else
            return await HttpResult.FromResponse(response, p => p.ReadAsStringAsync())
                    .SelectFromJsonContent<TResult>();
#endif

        }
        catch (Exception ex) { return HttpResult.FromException<TResult>(ex); }
    }

    /// <summary>
    /// Sends a POST request to the specified Uri with given model
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <typeparam name="TError"></typeparam>
    /// <param name="client"></param>
    /// <param name="entity"></param>
    /// <param name="requestUri"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<HttpResult<TResult, TError>> PostXAsync<TModel, TResult, TError>(this HttpClient client, TModel? entity, string requestUri, CancellationToken cancellationToken = default) where TModel : class
    {
        try
        {
            var stringContent = ToStringContent(entity);
            var response = await client.PostAsync(requestUri, stringContent, cancellationToken);
            return await HttpResult.FromJsonResponse<TResult, TError>(response, cancellationToken);
        }
        catch (Exception ex)
        {
            return HttpResult.FromException<TResult, TError>(ex);
        }
    }

    /// <summary>
    /// Deserialize string result
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="result"></param>
    /// <returns></returns>
    public static HttpResult<T> SelectFromJsonContent<T>(this HttpResult<string> result) => result.Select(Utility.DeserializeObject<T>);

    /// <summary>
    /// Deserialize string result
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="task"></param>
    /// <returns></returns>
    public static async Task<HttpResult<T>> SelectFromJsonContent<T>(this Task<HttpResult<string>> task)
    {
        try
        {
            var result = await task;
            return result.SelectFromJsonContent<T>();
        }
        catch (Exception ex) { return HttpResult.FromException<T>(ex); }
    }

    [return: NotNullIfNotNull(nameof(model))]
    public static StringContent? ToStringContent<T>(this T? model, Encoding? encoding = null)
    {
        if (model is null)
            return null;

        var content = Utility.Serialize(model);
        return new StringContent(content, encoding ?? Encoding.UTF8, "application/json");
    }
}