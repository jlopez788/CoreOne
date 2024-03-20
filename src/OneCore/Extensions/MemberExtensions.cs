namespace OneCore.Extensions;

public static class MemberExtensions
{
    public static bool AttributeExists<T>(this MemberInfo? member, bool inherit = false) where T : Attribute => member?.GetAttribute<T>(inherit) != null;

    public static T? GetAttribute<T>(this MemberInfo? member, bool inherit = false) where T : Attribute => member?.GetAttributes<T>(inherit)?.FirstOrDefault();

    public static IEnumerable<T> GetAttributes<T>(this MemberInfo? member, bool inherit = false) where T : Attribute => member?.GetCustomAttributes(typeof(T), inherit)?.Cast<T>() ?? [];

    public static bool TryGetAttribute<T>(this MemberInfo member, [NotNullWhen(true)] out T? attribute, bool inherit = true) where T : Attribute
    {
        attribute = member.GetAttribute<T>(inherit);
        return attribute != null;
    }
}