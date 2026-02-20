using System.Collections.Concurrent;

namespace CoreOne.Services;

public sealed class ModelTransaction : IDisposable
{
    private interface ITargetObjectResolver
    {
        object? Resolve(object? source, ObjectPath path);
    }

    private record ObjectPath(string Path, int Depth = 0)
    {
        public static readonly ObjectPath Root = new(string.Empty, 0);
        public ObjectPath Next(string path) => new(string.IsNullOrEmpty(Path) ? path : $"{Path}.{path}", Depth + 1);
    }

    private class CreateInstanceResolver : ITargetObjectResolver
    {
        public object? Resolve(object? source, ObjectPath path)
        {
            if (path.Depth == 10)
                return null;

            object? target = null;
            var type = source?.GetType();
            if (type?.IsArray == true && source is Array array)
            {
                var length = array.Length;
                target = Activator.CreateInstance(type, length);
            }
            else if (type is not null)
            {
#if NET9_0_OR_GREATER
                try
                { //no constructor calling, so no side effects during instantiation
                    target = TypeUtility.GetUninitializedInstance(type);
                }
                catch
                {
                    return source;
                }
#endif
            }
            return target;
        }
    }

    private class DataContext : Data<ObjectPath, object?>
    {
        public bool Readonly { get; set; }

        public override Data<ObjectPath, object?> Set(ObjectPath? key, object? value) => Readonly ? this : base.Set(key, value);
    }

    private class DictionaryRefResolver(DataContext refHolder) : ITargetObjectResolver
    {
        private readonly DataContext ReferenceHolder = refHolder;

        public object? Resolve(object? source, ObjectPath path)
        {
            if (path is null && source is not null)
                return source;
            var value = ReferenceHolder.Get(path);
            return value ?? source;
        }
    }

    private static readonly ConcurrentDictionary<Type, FieldInfo[]> Fields = new(1, 50);
    private static readonly Type TransactionType = typeof(ModelTransaction);
    private static readonly Type DbConnectionType = typeof(System.Data.IDbConnection);
    public object Model;
    private readonly DataContext Visited;
    private bool IsDisposed;

    public ModelTransaction(object model)
    {
        Visited = new();
        Model = model;
        ProcessRecursive(model.Copy(), Visited, new CreateInstanceResolver(), ObjectPath.Root, []);
    }

    public void Commit()
    {
        if (IsDisposed)
            return;

        //do nothing
        Clear();
    }

    public void Dispose()
    {
        Rollback();
    }

    public void Rollback()
    {
        if (IsDisposed)
            return;

        Visited.Readonly = true;
        var resolver = new DictionaryRefResolver(Visited);
        Model = ProcessRecursive(Model, Visited, resolver, ObjectPath.Root, [])!;

        Clear();
    }

    private static FieldInfo[] GetFields(Type? type)
    {
        var fields = type?.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        return fields?.Where(p => p.FieldType != TransactionType).ToArray() ?? [];
    }

    private void Clear()
    {
        IsDisposed = true;
    }

    private static object? ProcessRecursive(object? source, DataContext visited, ITargetObjectResolver resolver, ObjectPath path, HashSet<object> seen)
    {
        if (source is null)
            return null;

        if (!seen.Add(source))
            return visited.Get(path); // Already seen this instance

        var type = source.GetType();
        if (path.Path is not null && visited.TryGetValue(path, out var value))
            return value!;
        if (type.IsPrimitive() || DbConnectionType.IsAssignableFrom(type))
        {
            visited.Set(path, source);
            return source;
        }

        var target = resolver.Resolve(source, path);
        if (!string.IsNullOrEmpty(path.Path) && target is not null)
            visited.Set(path, target);

        if (type.IsArray)
        {
            var sourceArray = (Array)source;
            var targetArray = (Array?)target;
            for (int i = 0; i < sourceArray.Length; ++i)
            {
                var targetValue = ProcessRecursive(sourceArray.GetValue(i), visited, resolver, path.Next($"{path}[{i}]"), seen);
                targetArray?.SetValue(targetValue, i);
            }
        }
        else if (target is not null)
        {
            var fields = Fields.GetOrAdd(type, GetFields);
            foreach (var field in fields)
            {
                var objSourceField = field.GetValue(source);
                var targetValue = ProcessRecursive(objSourceField, visited, resolver, path.Next($"{path}.{field.Name}"), seen);
                try
                {
                    field.SetValue(target, targetValue);
                }
                catch
                {
                    Console.WriteLine("err");
                }
            }
        }

        return target;
    }
}