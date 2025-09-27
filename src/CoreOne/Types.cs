namespace CoreOne;

public static class Types
{
    public static readonly Lazy<Data<Type, MulticastDelegate>> LookupTryParse = new(Initialize);

    public static readonly HashSet<Type> NumericTypes = [
        typeof(int),
        typeof(double),
        typeof(decimal),
        typeof(long),
        typeof(short),
        typeof(sbyte),
        typeof(byte),
        typeof(ulong),
        typeof(ushort),
        typeof(uint),
        typeof(float)
    ];

    public static readonly Type
        Void = typeof(void),
        Object = typeof(object),
        Objects = typeof(object[]),
        Bytes = typeof(byte[]),
        Int = typeof(int),
        Long = typeof(long),
        NInt = typeof(int?),
        Guid = typeof(Guid),
        NGuid = typeof(Guid?),
        Bool = typeof(bool),
        NBool = typeof(bool?),
        NullableT = typeof(Nullable<>),
        DateTime = typeof(DateTime),
        String = typeof(string),
        Result = typeof(Result),
        ResultT = typeof(Result<>),
        IList = typeof(IList),
        ListT = typeof(List<>),
        Task = typeof(Task),
        TaskT = typeof(Task<>),
        Delegate = typeof(Delegate),
        EnumerableT = typeof(IEnumerable<>),
        CollectionT = typeof(ICollection<>),
        IDisposable = typeof(IDisposable),
        IAsyncDisposable = typeof(IAsyncDisposable);

    private static readonly Lazy<HashSet<Type>> CoreDotNetTypes = new(InitiallizeTypes);
    public static HashSet<Type> DotNetTypes => CoreDotNetTypes.Value;

    public static Type GetBasicType(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == NullableT)
            type = type.GetGenericArguments()[0];

        return type;
    }

    /// <summary>
    /// Checks if given type is one of predefined numeric types
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsNumberType(Type? type) => type is not null && NumericTypes.Contains(Nullable.GetUnderlyingType(type) ?? type);

    public static IResult<T> Parse<T>(object? value)
    {
        if (value is T t)
            return new Result<T>(t);

        var type = typeof(T);
        var ntype = Nullable.GetUnderlyingType(type);
        var result = new Result<T>(ResultType.Fail, "TryParse method not supported");
        if (LookupTryParse.Value.TryGetValue(ntype ?? type, out var cast))
        {
            object?[] args = [value?.ToString(), null];
            var isparsed = cast?.DynamicInvoke(args);
            if (isparsed is bool flag && flag)
                result = new Result<T>((T?)args[1]);
        }

        return result;
    }

    public static IResult<T> ParseEnum<T>(object? value) where T : struct
    {
        return value is T t ?
            new Result<T>(t) :
            TryParseEnum<T>(value?.ToString(), typeof(T), out var parsed) ?
            new Result<T>((T)parsed, true, ResultType.Success) :
            new Result<T>(ResultType.Fail, $"Invalid value: {value}");
    }

    public static IResult<object?> Parse(Type? type, object? value)
    {
        if (type is null || value is null)
            return new Result<object?>(ResultType.Fail, "Invalid type");

        var method = type.IsEnum == true ?
            typeof(Types).GetMethod(nameof(ParseEnum), [Object]) :
            typeof(Types).GetMethod(nameof(Parse), [Object]);
        method = method?.MakeGenericMethod(type);

        try
        {
            var result = method?.Invoke(null, [value]);
            if (result is not null)
            {
                var meta = MetaType.GetMetadatas(result.GetType()).ToDictionary();
                var ovalue = meta.Get(nameof(IResult<>.Model)).GetValue(result);
                var rtype = meta.Get(nameof(IResult<>.ResultType)).GetValue(result) is ResultType t ? t : ResultType.Fail;
                return new Result<object?>(ovalue, rtype);
            }
        }
        catch (Exception ex)
        {
            return Results.Result.FromException<object?>(ex);
        }
        return new Result<object?>(ResultType.Fail, "");
    }

    public static bool TryParse<T>(string? value, [NotNullWhen(true)] out T? parsedValue)
    {
        var result = Parse<T>(value);
        parsedValue = result.Model;
        return result.Success && parsedValue is not null;
    }

    public static bool TryParseEnum<TValue>(string? input, Type conversionType, [NotNullWhen(true)] out TValue? theEnum)
#if !NET9_0_OR_GREATER
        where TValue : struct
#endif
    {
        theEnum = default;
        if (!string.IsNullOrEmpty(input) &&
#if NET9_0_OR_GREATER
            Enum.TryParse(conversionType, input, true, out var parsed) &&
#else
             Enum.TryParse<TValue>(input, true, out var parsed) &&
#endif
            parsed is TValue tv)
        {
            theEnum = tv;
            return true;
        }

        return false;
    }

    public static bool TryParseEnum<TValue>(string? input, [NotNullWhen(true)] out TValue? theEnum)
#if !NET9_0_OR_GREATER
        where TValue : struct
#endif
    {
        var parsed = TryParseEnum<TValue>(input, typeof(TValue), out var entry);
        theEnum = parsed ? entry : default;
        return parsed;
    }

    private static Data<Type, MulticastDelegate> Initialize()
    {
        var data = new Data<Type, MulticastDelegate> {
            [typeof(bool)] = new TryParseDelegate<bool>(TryParseBoolean),
            [typeof(byte)] = new TryParseDelegate<byte>(byte.TryParse),
            [typeof(sbyte)] = new TryParseDelegate<sbyte>(sbyte.TryParse),
            [typeof(short)] = new TryParseDelegate<short>(short.TryParse),
            [typeof(ushort)] = new TryParseDelegate<ushort>(ushort.TryParse),
            [typeof(int)] = new TryParseDelegate<int>(int.TryParse),
            [typeof(uint)] = new TryParseDelegate<uint>(uint.TryParse),
            [typeof(long)] = new TryParseDelegate<long>(long.TryParse),
            [typeof(ulong)] = new TryParseDelegate<ulong>(ulong.TryParse),
            [typeof(double)] = new TryParseDelegate<double>(double.TryParse),
            [typeof(float)] = new TryParseDelegate<float>(float.TryParse),
            [typeof(decimal)] = new TryParseDelegate<decimal>(decimal.TryParse),
            [typeof(DateTime)] = new TryParseDelegate<DateTime>(TryParseDateTime),
            [typeof(TimeSpan)] = new TryParseDelegate<TimeSpan>(TimeSpan.TryParse),
            [typeof(Guid)] = new TryParseDelegate<Guid>(System.Guid.TryParse),
        };
        var keys = data.Keys.ToArray();
        var nullable = typeof(Nullable<>);
        keys.Each(p => {
            var n = nullable.MakeGenericType(p);
            data.Set(n, data[p]!);
        });
        return data;
    }

    private static HashSet<Type> InitiallizeTypes() => [
            typeof(long), typeof(long?), typeof(short), typeof(short?),
            typeof(ulong), typeof(ulong?), typeof(ushort), typeof(ushort?),
            typeof(float), typeof(float?), typeof(double), typeof(double?),
            typeof(decimal), typeof(decimal?), typeof(uint), typeof(uint?),
            typeof(int), typeof(int?), typeof(bool), typeof(bool?), typeof(string),
            typeof(DateTime), typeof(DateTime?), typeof(Guid), typeof(Guid?)
#if NET9_0_OR_GREATER
            ,typeof(DateOnly), typeof(DateOnly?)
#endif
        ];

    private static bool TryParseBoolean(string? str, out bool flag)
    {
        var parsed = false;
        flag = false;
        var value = str?.ToString();
        if (!string.IsNullOrEmpty(value))
        {
            var flags = new string[] { "1", "true", "yes", "t", "y", "0", "false", "f", "n", "no" };
            var idx = Array.IndexOf(flags, value!.ToLower());
            if (idx == -1)
            {
                return false;
            }
            parsed = idx >= 0;
            flag = idx is >= 0 and <= 4; // True uses index 0-3
        }
        return parsed;
    }

    private static bool TryParseDateTime(string? value, out DateTime date)
    {
        var dates = new string[] { "M/d/yy", "M/d/yyyy", "MM/dd/yyyy", "MM-dd-yyyy", "MM.dd.yyyy" };
        var times = new string[] { "HH:mm", "H:m", "H:mm" };
        var formats = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "d", "g" };
        formats.AddRange(dates);
        dates.Each(d => times.Each(t => formats.Add($"{d} {t}")));
        return System.DateTime.TryParseExact(value?.ToString() ?? string.Empty, [.. formats], null, DateTimeStyles.None, out date);
    }
}

public delegate bool TryParseDelegate<T>(string? str, [MaybeNullWhen(false)] out T result);