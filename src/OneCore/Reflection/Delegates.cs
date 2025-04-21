namespace CoreOne.Reflection;

public delegate object CreateInstance<in T1, in T2, in T3>(T1 t1, T2 t2, T3 t3);

public delegate object CreateInstance(object?[]? args);

public delegate object? Get(object instance);

public delegate object InvokeReturn(object instance, object?[]? args);

public delegate object InvokeStatic(object?[]? args);

public delegate void InvokeStaticVoid(object?[]? args);

public delegate void InvokeVoid(object instance, object?[]? args);

public delegate void Set(object instance, object? value);