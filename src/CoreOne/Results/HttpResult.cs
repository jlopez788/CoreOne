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
            if (response.IsSuccessStatusCode)
            {
#if NET9_0_OR_GREATER
                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
#else
                using var stream = await response.Content.ReadAsStreamAsync();
#endif
                var result = Utility.DeserializeObject<TModel>(stream);
                return new HttpResult<TModel, TError>(result.Model, status);
            }

            byte[]? errorBytes = null;
            if (response.Content is not null)
            {
#if NET9_0_OR_GREATER
                errorBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
#else
                errorBytes = await response.Content.ReadAsByteArrayAsync();
#endif
            }

            using var errorStream = new MemoryStream(errorBytes ?? []);
            var resultError = Utility.DeserializeObject<TError>(errorStream);
            return new HttpResult<TModel, TError>(resultError.Model, status) {
                Message = $"Error Code: {status}",
                ErrorContent = errorBytes ?? []
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
                    return new HttpResult<TModel>((TModel?)(object?)str, status, uri) {
                        Serializer = serializer
                    };
                }

                if (typeof(TModel) == Types.Bytes)
                {
#if NET9_0_OR_GREATER
                    var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
#else
                    var bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
#endif
                    return new HttpResult<TModel>((TModel?)(object?)bytes, status, uri) {
                        Serializer = serializer
                    };
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
                        Uri = uri,
                        Serializer = serializer
                    };
                }
                return new HttpResult<TModel>(result.Model is TModel typed ? typed : default, status, uri) {
                    Serializer = serializer
                };
            }

            byte[]? errorBytes = null;
            if (response.Content is not null)
            {
#if NET9_0_OR_GREATER
                errorBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
#else
                errorBytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
#endif
            }

            return new HttpResult<TModel>(status, $"Error Code: {status}") {
                Uri = uri,
                ErrorContent = errorBytes ?? [],
                Serializer = serializer
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
            if (response.IsSuccessStatusCode)
            {
#if NET9_0_OR_GREATER
                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#else
                using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
                var result = await serializer.DeserializeAsync(stream, typeof(TModel), cancellationToken).ConfigureAwait(false);
                if (!result.Success)
                {
                    return new HttpResult<TModel, TError>(WebCodes.Status500InternalServerError, result.Message ?? "Failed to deserialize response") {
                        Uri = uri,
                        Serializer = serializer
                    };
                }
                return new HttpResult<TModel, TError>(result.Model is TModel typed ? typed : default, status) {
                    Uri = uri,
                    Serializer = serializer
                };
            }

            byte[]? errorBytes = null;
            if (response.Content is not null)
            {
#if NET9_0_OR_GREATER
                errorBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
#else
                errorBytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
#endif
            }

            using var errorStream = new MemoryStream(errorBytes ?? []);
            var resultError = await serializer.DeserializeAsync(errorStream, typeof(TError), cancellationToken).ConfigureAwait(false);
            return new HttpResult<TModel, TError>(resultError.Model is TError typedError ? typedError : default, status) {
                Message = $"Error Code: {status}",
                Uri = uri,
                ErrorContent = errorBytes ?? [],
                Serializer = serializer
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

            byte[]? errorBytes = null;
            if (response.Content is not null)
            {
                errorBytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }

            return new HttpResult<T>(status, $"Error Code: {status}") {
                ErrorContent = errorBytes ?? []
            };
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

    public byte[]? ErrorContent { get; init; }
    public bool IsSuccessStatusCode { get; protected set; }
    public string? Message { get; set; }
    public ResultType ResultType { get; protected set; }
    protected internal ISerializer? Serializer { get; init; }
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

    public virtual IResult<TError> GetContent<TError>(ISerializer? serializer = null)
    {
        var activeSerializer = serializer ?? Serializer ?? NJsonService.Instance;
        var bytes = ErrorContent;
        if (bytes is null && !string.IsNullOrWhiteSpace(Message))
        {
            bytes = activeSerializer.Encoding.GetBytes(Message);
        }

        if (bytes is null || bytes.Length == 0)
            return new Result<TError>(default, "Error content is empty.", ResultType.Fail);

        if (typeof(TError) == Types.String)
        {
            var str = activeSerializer.Encoding.GetString(bytes);
            return new Result<TError>((TError?)(object?)str);
        }

        if (typeof(TError) == Types.Bytes)
        {
            return new Result<TError>((TError?)(object?)bytes);
        }

        var deserialized = activeSerializer.Deserialize(bytes, typeof(TError));
        if (!deserialized.Success)
            return new Result<TError>(default, deserialized.Message ?? "Failed to deserialize error content.", deserialized.ResultType);

        return deserialized.Model is TError typed
            ? new Result<TError>(typed)
            : new Result<TError>(default, "Failed to deserialize error content.", ResultType.Fail);
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

    public HttpResult<TResult> Select<TResult>(Func<T?, TResult?> next) => Success
        ? new HttpResult<TResult>(next(Model), StatusCode, Uri) { Serializer = Serializer }
        : new HttpResult<TResult>(StatusCode, Message) { Uri = Uri, ErrorContent = ErrorContent, Serializer = Serializer };
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

    public override IResult<TTarget> GetContent<TTarget>(ISerializer? serializer = null)
    {
        if (!Success && ErrorModel is TTarget typed)
            return new Result<TTarget>(typed);

        return base.GetContent<TTarget>(serializer);
    }
}