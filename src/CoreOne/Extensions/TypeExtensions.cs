using System.Runtime.CompilerServices;

namespace CoreOne.Extensions;

public static class TypeExtensions
{
    private static readonly Data<Type, Func<object>> Default = new(15);
    private static readonly Type LazyType = typeof(Lazy<>);

    /// <summary>
    /// Checks if attribute exists for given type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="type"></param>
    /// <param name="inherit"></param>
    /// <returns></returns>
    public static bool AttributeExists<T>(this Type type, bool inherit = false) where T : Attribute => type.GetAttributes<T>(inherit).Any();

    /// <summary>
    /// Gets attributes from type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="type"></param>
    /// <param name="inherit"></param>
    /// <returns></returns>
    public static IEnumerable<T> GetAttributes<T>(this Type type, bool inherit = false) where T : Attribute => type.GetCustomAttributes(typeof(T), inherit).Cast<T>();

    /// <summary>
    /// Gets the default value for given type
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static object? GetDefault(this Type? type)
    {
        if (type == null || type == Types.Void)
            return null;

        if (!Default.ContainsKey(type))
        {
            type = type.IsNullable() ? Nullable.GetUnderlyingType(type) : type;
            if (type != null)
            {
                var value = Expression.Convert(Expression.Default(type), Types.Object);
                var lambda = Expression.Lambda<Func<object>>(value);
                Default.Set(type, lambda.Compile());
            }
        }
        return Default.Get(type)?.Invoke();
    }

    /// <summary>
    /// Checks if type inherits from given interface
    /// </summary>
    /// <param name="type"></param>
    /// <param name="abstractType"></param>
    /// <returns></returns>
    public static bool Implements(this Type? type, Type? abstractType)
    {
        bool flag = (type != null) && (abstractType != null);
        if (flag)
        {
            flag = abstractType!.IsAssignableFrom(type);
            if (abstractType.IsGenericType && !flag)
            {
                flag = type!.GetInterfaces()
                    .Where(t => t.IsGenericType)
                    .Any(t => t.GetGenericTypeDefinition() == abstractType);
            }
        }
        return flag;
    }

    /// <summary>
    /// Checks if type is anonymous
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsAnonymous(this Type type)
    {
        if (type.IsGenericType)
        {
            var d = type.GetGenericTypeDefinition();
            if (d.IsClass && d.IsSealed && d.Attributes.HasFlag(TypeAttributes.NotPublic))
            {
                return d.AttributeExists<CompilerGeneratedAttribute>();
            }
        }
        return false;
    }

    /// <summary>
    /// Checks if type is from generic list
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsGenericList(this Type? type) => type != null && type.IsGenericType && type.GetGenericTypeDefinition() == Types.ListT;

    /// <summary>
    /// Check if type is <see cref="Lazy{T}"/>
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsLazyType(this Type? type) => type?.IsGenericType == true && type.GetGenericTypeDefinition() == LazyType;

    /// <summary>
    /// Checks if type is nullable
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsNullable(this Type? type) => type is not null && type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Nullable<>));

    /// <summary>
    /// Checks if type is primitive
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsPrimitive(this Type? type) => type is not null && (type.IsEnum || type == Types.String || (type.IsValueType && type.IsPrimitive) || Types.DotNetTypes.Contains(type));
}