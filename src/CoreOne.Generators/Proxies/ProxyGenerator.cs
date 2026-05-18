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
        // Filter classes with [InterceptedBy] attribute
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

    private static void GenerateMethodOverride(CSharpFileWriter writer, IMethodSymbol method)
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
            writer.WriteLine("var invocation = new Invocation");
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
            writer.WriteLine("Func<IInvocation, Task<object?>> next = (inv) => inv.ProceedAsync();");
            using (writer.BeginBlock("for (int i = _interceptors.Length - 1; i >= 0; i--)"))
            {
                writer.WriteLine("var interceptor = _interceptors[i];");
                writer.WriteLine("var currentNext = next;");
                writer.WriteLine("next = (inv) => interceptor.InterceptAsync(new Invocation");
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
        var interceptorTypes = GetInterceptorTypes(classSymbol);
        if (interceptorTypes.Count == 0)
            return;

        var methodsToIntercept = GetMethodsToIntercept(classSymbol).ToList();

        var writer = new CSharpFileWriter();
        writer.WriteLine("// <auto-generated/>");
        writer.WriteLine("#nullable enable");
        writer.WriteLine("using System;");
        writer.WriteLine("using System.Reflection;");
        writer.WriteLine("using System.Threading.Tasks;");
        writer.WriteLine("using CompileTimeAop.Core;");
        writer.WriteLine();

        var namespaceBlock = namespaceName != null ? writer.BeginBlock($"namespace {namespaceName}") : null;

        using (writer.BeginBlock($"public partial class {proxyClassName} : {className}"))
        {
            // Static caching of MethodInfo
            foreach (var method in methodsToIntercept)
            {
                var safeMethodName = GetSafeMethodName(method);
                writer.WriteLine($"private static readonly MethodInfo _{safeMethodName}Method = GetMethodInfo_{safeMethodName}();");
            }
            writer.WriteLine();

            foreach (var method in methodsToIntercept)
            {
                var safeMethodName = GetSafeMethodName(method);
                using (writer.BeginBlock($"private static MethodInfo GetMethodInfo_{safeMethodName}()"))
                {
                    if (method.IsGenericMethod)
                    {
                        using (writer.BeginBlock($"foreach (var m in typeof({classSymbol.ToDisplayString()}).GetMethods(BindingFlags.Public | BindingFlags.Instance))"))
                        {
                            writer.WriteLine($"if (m.Name == \"{method.Name}\" && m.IsGenericMethod && m.GetParameters().Length == {method.Parameters.Length}) return m;");
                        }
                        writer.WriteLine("throw new InvalidOperationException(\"Could not find method\");");
                    }
                    else
                    {
                        var paramTypes = string.Join(", ", method.Parameters.Select(p => $"typeof({p.Type.ToDisplayString()})"));
                        writer.WriteLine($"return typeof({classSymbol.ToDisplayString()}).GetMethod(\"{method.Name}\", BindingFlags.Public | BindingFlags.Instance, null, new Type[] {{ {paramTypes} }}, null)!;");
                    }
                }
            }
            writer.WriteLine();

            writer.WriteLine("private readonly IAsyncInterceptor[] _interceptors;");
            writer.WriteLine();
            using (writer.BeginBlock($"public {proxyClassName}({string.Join(", ", interceptorTypes.Select((t, i) => $"{t} interceptor{i}"))})"))
            {
                writer.WriteLine($"_interceptors = new IAsyncInterceptor[] {{ {string.Join(", ", interceptorTypes.Select((_, i) => $"interceptor{i}"))} }};");
            }

            foreach (var method in methodsToIntercept)
            {
                GenerateMethodOverride(writer, method);
            }
        }

        namespaceBlock?.Dispose();

        context.AddSource($"{proxyClassName}.g.cs", writer.ToSourceText());
    }

    private static List<string> GetInterceptorTypes(INamedTypeSymbol classSymbol)
    {
        var interceptors = new List<string>();
        foreach (var attr in classSymbol.GetAttributes())
        {
            var attrClass = attr.AttributeClass;
            if (attrClass == null)
                continue;

            if (attrClass.ToDisplayString() == "CoreOne.Attributes.InterceptedByAttribute")
            {
                // Non-generic form: [InterceptedBy(typeof(T))]
                if (attr.ConstructorArguments.Length > 0
                    && attr.ConstructorArguments[0].Value is INamedTypeSymbol typeArg)
                {
                    interceptors.Add(typeArg.ToDisplayString());
                }
            }
            else if (attrClass.BaseType?.ToDisplayString() == "CoreOne.Attributes.InterceptedByAttribute"
                     && attrClass.TypeArguments.Length > 0)
            {
                // Generic form: [InterceptedBy<T>]
                if (attrClass.TypeArguments[0] is INamedTypeSymbol genericTypeArg)
                {
                    interceptors.Add(genericTypeArg.ToDisplayString());
                }
            }
        }
        return interceptors;
    }

    private static IEnumerable<IMethodSymbol> GetMethodsToIntercept(INamedTypeSymbol classSymbol)
    {
        return classSymbol.GetMembers().OfType<IMethodSymbol>()
            .Where(m => m.IsVirtual || m.IsOverride)
            .Where(m => m.MethodKind == MethodKind.Ordinary);
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
                    if (fullName == "CoreOne.Attributes.InterceptedByAttribute"
                        || baseFullName == "CoreOne.Attributes.InterceptedByAttribute")
                    {
                        return classDeclaration;
                    }
                }
            }
        }

        return null;
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax classDeclaration &&
               classDeclaration.AttributeLists.Count > 0;
    }
}
