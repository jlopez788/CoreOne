namespace CoreOne.Results;

public class HttpResult : IResult, IStatusResult<int>
{
    #region -- Static --

    public static HttpResult FromException(Exception ex) => new(WebCodes.Status500InternalServerError, ex.InnerException?.Message ?? ex.Message);

    public static HttpResult<T> FromException<T>(Exception ex) => new(WebCodes.Status500InternalServerError, ex.InnerException?.Message ?? ex.Message);

    public static HttpResult<TModel, TError> FromException<TModel, TError>(Exception ex) => new(WebCodes.Status500InternalServerError, ex.InnerException?.Message ?? ex.Message);

    public static async Task<HttpResult<TModel, TError>> FromJsonResponse<TModel, TError>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var status = (int)response.StatusCode;
#if NET9_0_OR_GREATER
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
#else
            using var stream = await response.Content.ReadAsStreamAsync();
#endif
            if (response.IsSuccessStatusCode)
            {
                var result = Utility.DeserializeObject<TModel>(stream);
                return new HttpResult<TModel, TError>(result.Model, status);
            }

            var resultError = Utility.DeserializeObject<TError>(stream);
            return new HttpResult<TModel, TError>(resultError.Model, status) {
                Message = $"Error Code: {status}"
            };
        }
        catch (Exception ex)
        {
            return new HttpResult<TModel, TError>(WebCodes.Status500InternalServerError, $"Error reading Http response: {ex.Message}");
        }
    }

    public static async Task<HttpResult<TModel>> FromResponseAsync<TModel>(HttpResponseMessage response, ISerializer serializer, CancellationToken cancellationToken = default)
    {
        try
        {
            var status = (int)response.StatusCode;
            var uri = response.RequestMessage?.RequestUri?.ToString();
            if (response.IsSuccessStatusCode)
            {
                if (typeof(TModel) == Types.String)
                {
#if NET9_0_OR_GREATER
                    var str = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
                    var str = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
                    return new HttpResult<TModel>((TModel?)(object?)str, status, uri);
                }

                if (typeof(TModel) == Types.Bytes)
                {
#if NET9_0_OR_GREATER
                    var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
#else
                    var bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
#endif
                    return new HttpResult<TModel>((TModel?)(object?)bytes, status, uri);
                }

#if NET9_0_OR_GREATER
                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#else
                using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
                var result = await serializer.DeserializeAsync(stream, typeof(TModel), cancellationToken).ConfigureAwait(false);
                if (!result.Success)
                {
                    return new HttpResult<TModel>(WebCodes.Status500InternalServerError, result.Message ?? "Failed to deserialize response") {
                        Uri = uri
                    };
                }
                return new HttpResult<TModel>(result.Model is TModel typed ? typed : default, status, uri);
            }

            return new HttpResult<TModel>(status, $"Error Code: {status}") {
                Uri = uri
            };
        }
        catch (Exception ex)
        {
            return new HttpResult<TModel>(WebCodes.Status500InternalServerError, $"Error reading Http response: {ex.Message}");
        }
    }

    public static async Task<HttpResult<TModel, TError>> FromResponseAsync<TModel, TError>(HttpResponseMessage response, ISerializer serializer, CancellationToken cancellationToken = default)
    {
        try
        {
            var status = (int)response.StatusCode;
            var uri = response.RequestMessage?.RequestUri?.ToString();
#if NET9_0_OR_GREATER
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#else
            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
            if (response.IsSuccessStatusCode)
            {
                var result = await serializer.DeserializeAsync(stream, typeof(TModel), cancellationToken).ConfigureAwait(false);
                if (!result.Success)
                {
                    return new HttpResult<TModel, TError>(WebCodes.Status500InternalServerError, result.Message ?? "Failed to deserialize response") {
                        Uri = uri
                    };
                }
                return new HttpResult<TModel, TError>(result.Model is TModel typed ? typed : default, status) {
                    Uri = uri
                };
            }

            var resultError = await serializer.DeserializeAsync(stream, typeof(TError), cancellationToken).ConfigureAwait(false);
            return new HttpResult<TModel, TError>(resultError.Model is TError typedError ? typedError : default, status) {
                Message = $"Error Code: {status}",
                Uri = uri
            };
        }
        catch (Exception ex)
        {
            return new HttpResult<TModel, TError>(WebCodes.Status500InternalServerError, $"Error reading Http response: {ex.Message}");
        }
    }

    public static async Task<HttpResult<T>> FromResponse<T>(HttpResponseMessage response, Func<HttpContent, Task<T>> callback)
    {
        try
        {
            var status = (int)response.StatusCode;
            if (response.IsSuccessStatusCode)
            {
                var model = await callback(response.Content);
                return new HttpResult<T>(model, status);
            }
            return new HttpResult<T>(status, $"Error Code: {status}");
        }
        catch (Exception ex)
        {
            return new HttpResult<T>(WebCodes.Status500InternalServerError, $"Error reading Http response: {ex.Message}");
        }
    }

    public static int GetStatusCode<T>(IResult<T> result) => result.ResultType switch {
        ResultType.Fail => WebCodes.Status400BadRequest,
        ResultType.Exception => WebCodes.Status500InternalServerError,
        ResultType.Success when result.Model is null => WebCodes.Status204NoContent,
        _ => WebCodes.Status200OK
    };

    public static int GetStatusCode(IResult result) => result.ResultType switch {
        ResultType.Fail => WebCodes.Status400BadRequest,
        ResultType.Exception => WebCodes.Status500InternalServerError,
        _ => WebCodes.Status204NoContent
    };

    #endregion -- Static --

    public bool IsSuccessStatusCode { get; protected set; }
    public string? Message { get; set; }
    public ResultType ResultType { get; protected set; }
    public int StatusCode { get; protected set; }
    public bool Success => ResultType == ResultType.Success && IsSuccessStatusCode;
    public string? Uri { get; init; }

    public HttpResult()
    { }

    public HttpResult(HttpResponseMessage response) : this((int)response.StatusCode) { }

    public HttpResult(int statusCode, string? msg = null)
    {
        Message = msg;
        StatusCode = statusCode;

        var (success, resultType) = GetResultFromStatusCode(statusCode);
        IsSuccessStatusCode = success;
        ResultType = resultType;
    }

    public static (bool success, ResultType resultType) GetResultFromStatusCode(int statusCode)
    {
        return statusCode switch {
            _ when statusCode is 100 or (>= 200 and <= 299) => (true, ResultType.Success),
            _ when statusCode is >= 400 and <= 499 => (false, ResultType.Fail),
            _ when statusCode >= 500 => (false, ResultType.Exception),
            _ => (false, ResultType.Fail)
        };
    }
}

public class HttpResult<T> : HttpResult, IResult<T>, IResult<T, int>
{
    #region -- Static --

    public static implicit operator T?(HttpResult<T> response) => (response is not null) ? response.Model : default;

    #endregion -- Static --

    public T? Model { get; protected set; }

    public HttpResult(T? model, int statusCode = 200, string? uri = null) : base(statusCode)
    {
        Model = model;
        Uri = uri;
    }

    public HttpResult(int statusCode, string? msg) : base(statusCode, msg)
    {
    }

    public HttpResult<TResult> Select<TResult>(Func<T?, TResult?> next) => Success ? new HttpResult<TResult>(next(Model), StatusCode) : new HttpResult<TResult>(StatusCode, Message);
}

public class HttpResult<TModel, TError> : HttpResult, IResult<TModel>, IResult<TModel, int>
{
    public TError? ErrorModel { get; protected set; }
    public TModel? Model { get; protected set; }

    public HttpResult(TModel? model, int statusCode = 200) : base(statusCode)
    {
        Model = model;
    }

    public HttpResult(TError? errorModel, int statusCode = 400) : base(statusCode)
    {
        ErrorModel = errorModel;
    }

    public HttpResult(int statusCode, string? msg) : base(statusCode, msg)
    {
        Message = msg;
    }
}