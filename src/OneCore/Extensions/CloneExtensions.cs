namespace CoreOne.Extensions;

public static class ArrayExtensions
{
    public static void ForEach(this Array array, Action<Array, int[]> action)
    {
        if (array.LongLength == 0)
            return;
        var walker = new ArrayTraverse(array);
        do
            action(array, walker.Position);
        while (walker.Step());
    }
}

internal class ArrayTraverse
{
    public int[] Position;
    private readonly int[] maxLengths;

    public ArrayTraverse(Array array)
    {
        maxLengths = new int[array.Rank];
        for (int i = 0; i < array.Rank; ++i)
        {
            maxLengths[i] = array.GetLength(i) - 1;
        }
        Position = new int[array.Rank];
    }

    public bool Step()
    {
        for (int i = 0; i < Position.Length; ++i)
        {
            if (Position[i] < maxLengths[i])
            {
                Position[i]++;
                for (int j = 0; j < i; j++)
                {
                    Position[j] = 0;
                }
                return true;
            }
        }
        return false;
    }
}

public static class CloneExtensions
{
    private static readonly MethodInfo? CloneMethod = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);

    [return: NotNullIfNotNull(nameof(original))]
    public static T? Copy<T>(this T? original) => (T?)InternalCopy(original, new Dictionary<object, object?>(ReferenceEqualityComparer.Default));

    private static void CopyFields(object originalObject, IDictionary<object, object?> visited, object? cloneObject, Type typeToReflect, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy, Func<FieldInfo, bool>? filter = null)
    {
        foreach (FieldInfo fieldInfo in typeToReflect.GetFields(bindingFlags))
        {
            if (filter != null && !filter(fieldInfo))
                continue;
            if (fieldInfo.FieldType.IsPrimitive())
                continue;
            var originalFieldValue = fieldInfo.GetValue(originalObject);
            var clonedFieldValue = InternalCopy(originalFieldValue, visited);
            fieldInfo.SetValue(cloneObject, clonedFieldValue);
        }
    }

    private static object? InternalCopy(object? originalObject, IDictionary<object, object?> visited)
    {
        if (originalObject is null)
            return null;
        var typeToReflect = originalObject.GetType();
        if (typeToReflect.IsPrimitive())
            return originalObject;
        if (visited.TryGetValue(originalObject, out object? value))
            return value;
        if (typeof(Delegate).IsAssignableFrom(typeToReflect))
            return null;
        var cloneObject = CloneMethod?.Invoke(originalObject, null);
        if (typeToReflect.IsArray)
        {
            var arrayType = typeToReflect.GetElementType();
            if (arrayType is not null && !arrayType.IsPrimitive())
            {
                var clonedArray = (Array?)cloneObject;
                clonedArray?.ForEach((array, indices) => array.SetValue(InternalCopy(clonedArray?.GetValue(indices), visited), indices));
            }
        }
        visited.Add(originalObject, cloneObject);
        CopyFields(originalObject, visited, cloneObject, typeToReflect);
        RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect);
        return cloneObject;
    }

    private static void RecursiveCopyBaseTypePrivateFields(object originalObject, IDictionary<object, object?> visited, object? cloneObject, Type typeToReflect)
    {
        if (typeToReflect.BaseType != null)
        {
            RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect.BaseType);
            CopyFields(originalObject, visited, cloneObject, typeToReflect.BaseType, BindingFlags.Instance | BindingFlags.NonPublic, info => info.IsPrivate);
        }
    }
}