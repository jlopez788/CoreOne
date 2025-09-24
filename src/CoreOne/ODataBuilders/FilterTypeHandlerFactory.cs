namespace CoreOne.ODataBuilders;

public class FilterTypeHandlerFactory : Data<Type, IFilterTypeHandler>
{
    public static readonly FilterTypeHandlerFactory Default = new(true);

    public FilterTypeHandlerFactory()
    { }

    private FilterTypeHandlerFactory(FilterTypeHandlerFactory factory) : base(factory) { }

    private FilterTypeHandlerFactory(bool _)
    {
        SetDefaultKey(Types.String);
        set(Types.Int, parse<int>());
        set(Types.Long, parse<long>());
        set(Types.Guid, parse<Guid>());
        set(Types.Bool, parse<bool>());
        set(Types.String, value => new FilterTypeResult(!string.IsNullOrEmpty(value), !string.IsNullOrEmpty(value), value, value));
        set(Types.DateTime, parse<DateTime>());

        void set(Type type, Func<string, FilterTypeResult> parse) => SetHandler(new FilterTypeHandler(type, parse, c => c.GetODataFilter()));
        Func<string, FilterTypeResult> parse<T>() => value => {
            var flag = Types.TryParse<T>(value, out var parsed);
            return new FilterTypeResult(flag, !string.IsNullOrEmpty(value), parsed, value);
        };
    }

    public FilterTypeHandlerFactory Clone() => new(this);

    public void SetHandler(IFilterTypeHandler handler)
    {
        if (handler is not null)
        {
            Set(handler.Type, handler);
        }
    }
}