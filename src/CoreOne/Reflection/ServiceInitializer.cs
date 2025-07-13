using CoreOne.Attributes;
using System.Diagnostics;

namespace CoreOne.Reflection;

public static class ServiceInitializer
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

    public static void Initialize(object instance, IServiceProvider? services)
    {
        if (services is null)
            return;

        var type = instance.GetType();
        var metas = MetaType.GetMetadatas(type, Flags)
                .ToDictionary();
        metas.Select(p => new {
            key = p.Key,
            meta = p.Value,
            attr = p.Value.GetCustomAttribute<ServiceAttribute>()
        }).Where(p => p.attr is not null)
            .Each(p => {
                var isnull = p.attr.DefaultServiceType is null;
                var service = services.Resolve(p.meta.FPType!);
                if (service is null && !isnull)
                    service = services.Resolve(p.attr.DefaultServiceType!);
                var set = p.meta.SetValue(instance, service);
                if (!set)
                {
                    var root = $"<{p.meta.Name}>k__BackingField";
                    var backing = metas.Get(root, () => Metadata.Empty);
                    if (backing == Metadata.Empty)
                        backing = metas.Get(p.meta.Name);

                    var field = backing.AsFieldInfo();
                    if (field is not null)
                    {
                        try
                        { field.SetValue(instance, service); }
                        catch
                        { Debugger.Break(); }
                    }
                }
            });
    }
}