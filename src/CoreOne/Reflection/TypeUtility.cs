namespace CoreOne.Reflection;

public static class TypeUtility
{
    public static bool IsSubclassOfRawGeneric(Type generic, Type? toCheck)
    {
        while (toCheck != null && toCheck != Types.Object)
        {
            var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
            if (generic == cur)
                return true;

            toCheck = toCheck.BaseType;
        }
        return false;
    }
}