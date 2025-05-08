using System.ComponentModel;

namespace CoreOne.Extensions;

public static class ComponentExtensions
{
    public static SToken CreateSToken(this IComponent component)
    {
        var token = SToken.Create();
        EventHandler dispose = default!;
        dispose = (s, e) => {
            component.Disposed -= dispose;
            token.Dispose();
        };
        component.Disposed += dispose;
        return token;
    }
}