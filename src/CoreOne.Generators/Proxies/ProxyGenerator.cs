using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace CoreOne.Generators.Proxies;

[Generator]
public class ProxyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Filter classes with [Intercept] attribute
        IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null)!;

        IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClasses =
            context.CompilationProvider.Combine(classDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndClasses, static (spc, source) => Execute(source.Item1, source.Item2, spc));
    }

    private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty)
            return;

        var distinctClasses = classes.Distinct().ToImmutableArray();
        foreach (var classDeclaration in distinctClasses)
        {
            var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol)
                continue;

            GenerateProxyClass(context, classSymbol);
        }
    }

    private static void GenerateConstructor(CSharpFileWriter writer, string proxyClassName, IMethodSymbol ctor, List<string> classInterceptorTypes, List<MethodInterceptInfo> methodInfos)
    {
        var baseParams = ctor.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}").ToList();
        var classInterceptorParams = classInterceptorTypes.Select((t, i) => $"{t} interceptor{i}");
        var methodInterceptorParams = methodInfos
            .Where(mi => mi.HasMethodLevel)
            .SelectMany(mi => mi.MethodLevelTypes.Select((t, i) => $"{t} {mi.SafeName}Interceptor{i}"));
        var allParams = baseParams.Concat(classInterceptorParams).Concat(methodInterceptorParams);
        var baseCall = ctor.Parameters.Length > 0
            ? $" : base({string.Join(", ", ctor.Parameters.Select(p => p.Name))})"
            : " : base()";

        using (writer.BeginBlock($"public {proxyClassName}({string.Join(", ", allParams)}){baseCall}"))
        {
            WriteConstructorBody(writer, classInterceptorTypes, methodInfos);
        }
    }

    private static void GenerateConstructors(CSharpFileWriter writer, string proxyClassName, INamedTypeSymbol classSymbol, List<string> classInterceptorTypes, List<MethodInterceptInfo> methodInfos)
    {
        writer.WriteLine();

        var accessibleCtors = classSymbol.InstanceConstructors
            .Where(c => c.DeclaredAccessibility is Accessibility.Public or Accessibility.Protected)
            .ToList();

        var classInterceptorParamDefs = classInterceptorTypes.Select((t, i) => $"{t} interceptor{i}");
        var methodInterceptorParamDefs = methodInfos
            .Where(mi => mi.HasMethodLevel)
            .SelectMany(mi => mi.MethodLevelTypes.Select((t, i) => $"{t} {mi.SafeName}Interceptor{i}"));
        var allInterceptorParams = methodInterceptorParamDefs.Concat(classInterceptorParamDefs);

        // Fall back to simple form when no explicit constructors exist (compiler-generated default ctor only)
        if (accessibleCtors.Count == 0 || accessibleCtors.All(c => c.IsImplicitlyDeclared))
        {
            using (writer.BeginBlock($"public {proxyClassName}({string.Join(", ", allInterceptorParams)})"))
            {
                WriteConstructorBody(writer, classInterceptorTypes, methodInfos);
            }
            return;
        }

        bool first = true;
        foreach (var ctor in accessibleCtors)
        {
            if (!first)
                writer.WriteLine();
            first = false;
            GenerateConstructor(writer, proxyClassName, ctor, classInterceptorTypes, methodInfos);
        }
    }

    private static void GenerateMethodOverride(CSharpFileWriter writer, IMethodSymbol method, string interceptorArrayName)
    {
        var returnType = method.ReturnType.ToDisplayString();
        var methodName = method.Name;
        var safeMethodName = GetSafeMethodName(method);
        var typeParameters = method.IsGenericMethod ? $"<{string.Join(", ", method.TypeParameters.Select(tp => tp.Name))}>" : "";
        var parameters = method.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}").ToList();
        var parameterNames = method.Parameters.Select(p => p.Name).ToList();
        bool isAsync = method.ReturnType.ToDisplayString().StartsWith("System.Threading.Tasks.Task");

        writer.WriteLine();
        using (writer.BeginBlock($"public override {(isAsync ? "async " : "")}{returnType} {methodName}{typeParameters}({string.Join(", ", parameters)})"))
        {
            writer.WriteLine("var invocation = new global::CoreOne.Models.Invocation");
            writer.WriteLine('{');
            writer.Indentation++;
            writer.WriteLine($"MethodName = \"{methodName}\",");
            writer.WriteLine($"Method = _{safeMethodName}Method,");
            writer.WriteLine($"Arguments = new object[] {{ {string.Join(", ", parameterNames)} }},");
            writer.WriteLine("ProceedAsync = async () =>");
            writer.WriteLine('{');
            writer.Indentation++;

            string baseCall = $"base.{methodName}{typeParameters}({string.Join(", ", parameterNames)})";
            if (method.ReturnsVoid)
            {
                writer.WriteLine($"{baseCall};");
                writer.WriteLine("return null;");
            }
            else if (returnType == "System.Threading.Tasks.Task")
            {
                writer.WriteLine($"await {baseCall};");
                writer.WriteLine("return null;");
            }
            else if (isAsync && method.ReturnType is INamedTypeSymbol taskType && taskType.IsGenericType)
            {
                writer.WriteLine($"return await {baseCall};");
            }
            else
            {
                writer.WriteLine($"return {baseCall};");
            }

            writer.Indentation--;
            writer.WriteLine("},");
            writer.Indentation--;
            writer.WriteLine("};");

            writer.WriteLine();
            writer.WriteLine("Func<global::CoreOne.IInvocation, Task<object?>> next = (inv) => inv.ProceedAsync();");
            using (writer.BeginBlock($"for (int i = {interceptorArrayName}.Length - 1; i >= 0; i--)"))
            {
                writer.WriteLine($"var interceptor = {interceptorArrayName}[i];");
                writer.WriteLine("var currentNext = next;");
                writer.WriteLine("next = (inv) => interceptor.InterceptAsync(new global::CoreOne.Models.Invocation");
                writer.WriteLine('{');
                writer.Indentation++;
                writer.WriteLine("MethodName = inv.MethodName,");
                writer.WriteLine("Method = inv.Method,");
                writer.WriteLine("Arguments = inv.Arguments,");
                writer.WriteLine("ProceedAsync = () => currentNext(inv)");
                writer.Indentation--;
                writer.WriteLine("});");
            }

            writer.WriteLine();
            if (isAsync)
            {
                writer.WriteLine("var result = await next(invocation);");
                if (!method.ReturnsVoid && returnType != "System.Threading.Tasks.Task")
                {
                    var innerType = (method.ReturnType as INamedTypeSymbol)?.TypeArguments[0].ToDisplayString() ?? "object";
                    writer.WriteLine($"return ({innerType})result!;");
                }
            }
            else
            {
                writer.WriteLine("var result = next(invocation).GetAwaiter().GetResult();");
                if (!method.ReturnsVoid)
                {
                    writer.WriteLine($"return ({returnType})result!;");
                }
            }
        }
    }

    private static void GenerateProxyClass(SourceProductionContext context, INamedTypeSymbol classSymbol)
    {
        var namespaceName = classSymbol.ContainingNamespace.IsGlobalNamespace ? null : classSymbol.ContainingNamespace.ToDisplayString();
        var className = classSymbol.Name;
        var proxyClassName = $"{className}Proxy";
        var classLevelTypes = GetInterceptorTypes(classSymbol);

        var methodInfos = classSymbol.GetMembers().OfType<IMethodSymbol>()
            .Where(m => (m.IsVirtual || m.IsOverride) && m.MethodKind == MethodKind.Ordinary)
            .Select(m => {
                var methodLevel = GetInterceptorTypes(m);
                var combined = new List<string>(methodLevel.Count + classLevelTypes.Count);
                combined.AddRange(methodLevel);
                combined.AddRange(classLevelTypes);
                return new MethodInterceptInfo(m, methodLevel, combined, GetSafeMethodName(m));
            })
            .Where(mi => mi.CombinedTypes.Count > 0)
            .ToList();

        if (methodInfos.Count == 0)
            return;

        var writer = new CSharpFileWriter();
        writer.WriteLine("// <auto-generated/>");
        writer.WriteLine("#nullable enable");
        writer.WriteLine("using CoreOne;");
        writer.WriteLine("using System;");
        writer.WriteLine("using System.Reflection;");
        writer.WriteLine("using System.Threading.Tasks;");
        writer.WriteLine();

        var namespaceBlock = namespaceName != null ? writer.BeginBlock($"namespace {namespaceName}") : null;

        using (writer.BeginBlock($"public partial class {proxyClassName} : {className}"))
        {
            // Static caching of MethodInfo
            foreach (var mi in methodInfos)
                writer.WriteLine($"private static readonly MethodInfo _{mi.SafeName}Method = GetMethodInfo_{mi.SafeName}();");
            writer.WriteLine();

            foreach (var mi in methodInfos)
            {
                using (writer.BeginBlock($"private static MethodInfo GetMethodInfo_{mi.SafeName}()"))
                {
                    if (mi.Method.IsGenericMethod)
                    {
                        using (writer.BeginBlock($"foreach (var m in typeof({classSymbol.ToDisplayString()}).GetMethods(BindingFlags.Public | BindingFlags.Instance))"))
                        {
                            using (writer.BeginBlock($"if (m.Name == \"{mi.Method.Name}\" && m.IsGenericMethod && m.GetParameters().Length == {mi.Method.Parameters.Length})"))
                            {
                                writer.WriteLine("return m;");
                            }
                        }
                        writer.WriteLine("throw new InvalidOperationException(\"Could not find method\");");
                    }
                    else
                    {
                        var paramTypes = string.Join(", ", mi.Method.Parameters.Select(p => $"typeof({p.Type.ToDisplayString()})"));
                        writer.WriteLine($"return typeof({classSymbol.ToDisplayString()}).GetMethod(\"{mi.Method.Name}\", BindingFlags.Public | BindingFlags.Instance, null, new Type[] {{ {paramTypes} }}, null)!;");
                    }
                }
            }
            writer.WriteLine();

            if (classLevelTypes.Count > 0)
                writer.WriteLine("private readonly IAsyncInterceptor[] _interceptors;");
            foreach (var mi in methodInfos.Where(m => m.HasMethodLevel))
                writer.WriteLine($"private readonly IAsyncInterceptor[] {mi.ArrayFieldName};");

            GenerateConstructors(writer, proxyClassName, classSymbol, classLevelTypes, methodInfos);

            foreach (var mi in methodInfos)
                GenerateMethodOverride(writer, mi.Method, mi.ArrayFieldName);
        }

        namespaceBlock?.Dispose();

        context.AddSource($"{proxyClassName}.g.cs", writer.ToSourceText());
    }

    private static List<string> GetInterceptorTypes(ISymbol symbol)
    {
        var interceptors = new List<string>();
        foreach (var attr in symbol.GetAttributes())
        {
            var attrClass = attr.AttributeClass;
            if (attrClass == null)
                continue;

            if (attrClass.ToDisplayString() == "CoreOne.Attributes.InterceptAttribute")
            {
                // Non-generic form: [Intercept(typeof(T))]
                if (attr.ConstructorArguments.Length > 0
                    && attr.ConstructorArguments[0].Value is INamedTypeSymbol typeArg)
                {
                    interceptors.Add(typeArg.ToDisplayString());
                }
            }
            else if (attrClass.BaseType?.ToDisplayString() == "CoreOne.Attributes.InterceptAttribute"
                     && attrClass.TypeArguments.Length > 0)
            {
                // Generic form: [Intercept<T>]
                if (attrClass.TypeArguments[0] is INamedTypeSymbol genericTypeArg)
                {
                    interceptors.Add(genericTypeArg.ToDisplayString());
                }
            }
        }
        return interceptors;
    }

    private static string GetSafeMethodName(IMethodSymbol method)
    {
        var name = method.Name;
        // Use parameter count and names to distinguish overloads
        name += "_" + string.Join("_", method.Parameters.Select(p => p.Type.Name));
        return name.Replace(".", "_").Replace("<", "").Replace(">", "").Replace("[]", "Array");
    }

    private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        foreach (var attributeList in classDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                if (context.SemanticModel.GetSymbolInfo(attribute).Symbol is IMethodSymbol attributeSymbol)
                {
                    var containingType = attributeSymbol.ContainingType;
                    var fullName = containingType.ToDisplayString();
                    var baseFullName = containingType.BaseType?.ToDisplayString();
                    if (fullName == "CoreOne.Attributes.InterceptAttribute"
                        || baseFullName == "CoreOne.Attributes.InterceptAttribute")
                    {
                        return classDeclaration;
                    }
                }
            }
        }

        // Also check if any method in the class has [Intercept]
        foreach (var member in classDeclaration.Members.OfType<MethodDeclarationSyntax>())
        {
            foreach (var attributeList in member.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    if (context.SemanticModel.GetSymbolInfo(attribute).Symbol is IMethodSymbol attributeSymbol)
                    {
                        var containingType = attributeSymbol.ContainingType;
                        var fullName = containingType.ToDisplayString();
                        var baseFullName = containingType.BaseType?.ToDisplayString();
                        if (fullName == "CoreOne.Attributes.InterceptAttribute"
                            || baseFullName == "CoreOne.Attributes.InterceptAttribute")
                        {
                            return classDeclaration;
                        }
                    }
                }
            }
        }

        return null;
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        if (node is not ClassDeclarationSyntax classDeclaration)
            return false;
        if (classDeclaration.AttributeLists.Count > 0)
            return true;
        return classDeclaration.Members
            .OfType<MethodDeclarationSyntax>()
            .Any(m => m.AttributeLists.Count > 0);
    }

    private static void WriteConstructorBody(CSharpFileWriter writer, List<string> classInterceptorTypes, List<MethodInterceptInfo> methodInfos)
    {
        if (classInterceptorTypes.Count > 0)
            writer.WriteLine($"_interceptors = new IAsyncInterceptor[] {{ {string.Join(", ", classInterceptorTypes.Select((_, i) => $"interceptor{i}"))} }};");

        foreach (var mi in methodInfos.Where(m => m.HasMethodLevel))
        {
            var methodParams = mi.MethodLevelTypes.Select((_, i) => $"{mi.SafeName}Interceptor{i}");
            var classParams = classInterceptorTypes.Select((_, i) => $"interceptor{i}");
            var combined = string.Join(", ", classParams.Concat(methodParams));
            writer.WriteLine($"{mi.ArrayFieldName} = new IAsyncInterceptor[] {{ {combined} }};");
        }
    }

    private sealed record MethodInterceptInfo(
        IMethodSymbol Method,
        List<string> MethodLevelTypes,
        List<string> CombinedTypes,
        string SafeName)
    {
        public bool HasMethodLevel => MethodLevelTypes.Count > 0;
        public string ArrayFieldName => HasMethodLevel ? $"_methodInterceptors_{SafeName}" : "_interceptors";
    }
}
