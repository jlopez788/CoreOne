using System.ComponentModel;
using System.Text;

namespace CoreOne.Extensions;

public static class MemberExtensions
{
    public static bool AttributeExists<T>(this MemberInfo? member, bool inherit = false) where T : Attribute => member?.GetAttribute<T>(inherit) != null;

    public static T? GetAttribute<T>(this MemberInfo? member, bool inherit = false) where T : Attribute => member?.GetAttributes<T>(inherit)?.FirstOrDefault();

    public static IEnumerable<T> GetAttributes<T>(this MemberInfo? member, bool inherit = false) where T : Attribute => member?.GetCustomAttributes(typeof(T), inherit)?.Cast<T>() ?? [];

    public static string GetDisplayLabel(this Metadata meta)
    {
        var names = new[] {
            getDisplayName,
            getDisplay,
            getDecamel
        };

        return names.Select(p => p.Invoke())
            .FirstOrDefault(p => p.IsNotNullOrEmpty()) ?? string.Empty;

        string? getDisplayName() => meta.TryGetAttribute<DisplayNameAttribute>(out var name) ? name?.DisplayName : null;
        string? getDisplay() => meta.TryGetAttribute<DisplayAttribute>(out var name) ? name.Name : null;
        string? getDecamel()
        {
            bool toUpper = true;
            var name = meta.Name;
            var builder = new StringBuilder();
            foreach (char c in name!)
            {
                if (c == '_')
                {
                    if (builder.Length > 0)
                        builder.Append(' ');

                    toUpper = true;
                    continue;
                }
                if (char.IsUpper(c) && builder.Length > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(toUpper ? char.ToUpper(c) : c);
                toUpper = false;
            }
            return builder.ToString();
        }
    }

    public static bool TryGetAttribute<T>(this MemberInfo member, [NotNullWhen(true)] out T? attribute, bool inherit = true) where T : Attribute
    {
        attribute = member.GetAttribute<T>(inherit);
        return attribute != null;
    }
}