using CoreOne.Generators.Proxies;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace CoreOne.Generators.Rest;

[Generator]
public class RestClientGenerator : IIncrementalGenerator
{
    private const string AliasAsAttributeName = "CoreOne.Http.Attributes.AliasAsAttribute";
    private const string AuthorizeAttributeName = "CoreOne.Http.Attributes.AuthorizeAttribute";
    private const string BodyAttributeName = "CoreOne.Http.Attributes.BodyAttribute";
    private const string HeaderAttributeName = "CoreOne.Http.Attributes.HeaderAttribute";
    private const string HeadersAttributeName = "CoreOne.Http.Attributes.HeadersAttribute";
    private const string HttpMethodAttributeName = "CoreOne.Http.Attributes.HttpMethodAttribute";
    private const string MultipartAttributeName = "CoreOne.Http.Attributes.MultipartAttribute";
    private const string QueryParamAttributeName = "CoreOne.Http.Attributes.QueryParamAttribute";
    private const string RestClientAttributeName = "CoreOne.Http.Attributes.RestClientAttribute";
    private const string SerializerAttributeName = "CoreOne.Http.Attributes.SerializerAttribute";

    private static readonly HashSet<string> HttpVerbMap = new(StringComparer.Ordinal) {
        "CoreOne.Http.Attributes.GetAttribute",
        "CoreOne.Http.Attributes.PostAttribute",
        "CoreOne.Http.Attributes.PutAttribute",
        "CoreOne.Http.Attributes.DeleteAttribute",
        "CoreOne.Http.Attributes.PatchAttribute",
        "CoreOne.Http.Attributes.HeadAttribute",
        "CoreOne.Http.Attributes.OptionsAttribute",
        "CoreOne.Http.Attributes.QueryAttribute"
    };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<InterfaceDeclarationSyntax> interfaceDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null)!;

        IncrementalValueProvider<(Compilation, ImmutableArray<InterfaceDeclarationSyntax>)> compilationAndInterfaces =
            context.CompilationProvider.Combine(interfaceDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndInterfaces, static (spc, source) => Execute(source.Item1, source.Item2, spc));
    }

    internal static string GetDefaultClientClassName(string interfaceName)
    {
        if (interfaceName.Length > 1 && interfaceName[0] == 'I' && char.IsUpper(interfaceName[1]))
            return $"{interfaceName.Substring(1)}Client";

        return $"{interfaceName}Client";
    }

    private static bool ContainsRouteToken(string routeTemplate, string tokenName)
    {
        if (string.IsNullOrEmpty(routeTemplate) || string.IsNullOrEmpty(tokenName))
            return false;

        return routeTemplate.IndexOf($"{{{tokenName}}}", StringComparison.OrdinalIgnoreCase) >= 0
            || routeTemplate.IndexOf($"{{*{tokenName}}}", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string EscapeStringLiteral(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static void Execute(Compilation compilation, ImmutableArray<InterfaceDeclarationSyntax> interfaces, SourceProductionContext context)
    {
        if (interfaces.IsDefaultOrEmpty)
            return;

        var distinctInterfaces = interfaces.Distinct().ToImmutableArray();
        foreach (var interfaceDeclaration in distinctInterfaces)
        {
            var semanticModel = compilation.GetSemanticModel(interfaceDeclaration.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(interfaceDeclaration) is not INamedTypeSymbol interfaceSymbol)
                continue;

            GenerateRestClient(context, interfaceSymbol);
        }
    }

    private static (string? Key, string? TypeName) ExtractSerializerInfo(ISymbol symbol)
    {
        foreach (var attr in symbol.GetAttributes())
        {
            var attrClass = attr.AttributeClass;
            if (attrClass is null)
                continue;

            var fullName = attrClass.ToDisplayString();
            if (fullName == SerializerAttributeName)
            {
                if (attr.ConstructorArguments.Length > 0)
                {
                    var arg = attr.ConstructorArguments[0];
                    if (arg.Value is string keyStr)
                        return (keyStr, null);
                    if (arg.Value is INamedTypeSymbol typeArg)
                        return (null, typeArg.ToDisplayString());
                }
            }
            else if (attrClass.BaseType?.ToDisplayString() == SerializerAttributeName && attrClass.TypeArguments.Length > 0)
            {
                if (attrClass.TypeArguments[0] is INamedTypeSymbol genericTypeArg)
                    return (null, genericTypeArg.ToDisplayString());
            }
        }
        return (null, null);
    }

    private static List<string> ExtractStaticHeaders(ISymbol symbol)
    {
        var headers = new List<string>();
        foreach (var attr in symbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == HeadersAttributeName && attr.ConstructorArguments.Length > 0)
            {
                var arg = attr.ConstructorArguments[0];
                if (arg.Kind == TypedConstantKind.Array)
                {
                    foreach (var item in arg.Values)
                    {
                        if (item.Value is string h && !string.IsNullOrWhiteSpace(h))
                            headers.Add(h);
                    }
                }
            }
        }
        return headers;
    }

    private static void GenerateMethodImplementation(
        CSharpFileWriter writer,
        IMethodSymbol method,
        string? basePath,
        List<string> interfaceHeaders,
        string httpMethod,
        string relativePath)
    {
        var returnType = method.ReturnType.ToDisplayString();
        var methodName = method.Name;
        var typeParameters = method.IsGenericMethod ? $"<{string.Join(", ", method.TypeParameters.Select(tp => tp.Name))}>" : "";
        var parameters = method.Parameters.Select(FormatParameter).ToList();

        var methodInterceptors = ProxyGenerator.GetInterceptorTypes(method);
        var (methodSerializerKey, methodSerializerType) = ExtractSerializerInfo(method);
        var methodHeaders = ExtractStaticHeaders(method);
        var allStaticHeaders = interfaceHeaders.Concat(methodHeaders).ToList();

        bool isMultipart = false;
        string multipartBoundary = "----CoreOneMultipartBoundary";
        foreach (var attr in method.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == MultipartAttributeName)
            {
                isMultipart = true;
                if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string b && !string.IsNullOrWhiteSpace(b))
                    multipartBoundary = b;
            }
        }

        writer.WriteLine();
        foreach (var interceptorType in methodInterceptors)
        {
            writer.WriteLine($"[global::CoreOne.Attributes.InterceptAttribute(typeof({interceptorType}))]");
        }

        using (writer.BeginBlock($"public virtual async {returnType} {methodName}{typeParameters}({string.Join(", ", parameters)})"))
        {
            string serializerVar = "Serializer";
            if (methodSerializerType is not null)
            {
                serializerVar = "_methodSerializer";
                writer.WriteLine($"var {serializerVar} = global::CoreOne.Http.Extensions.SerializerHttpExtensions.ResolveSerializer(Services, null, typeof({methodSerializerType}), Serializer);");
            }
            else if (!string.IsNullOrEmpty(methodSerializerKey))
            {
                serializerVar = "_methodSerializer";
                writer.WriteLine($"var {serializerVar} = global::CoreOne.Http.Extensions.SerializerHttpExtensions.ResolveSerializer(Services, \"{EscapeStringLiteral(methodSerializerKey!)}\", null, Serializer);");
            }

            var basePathLiteral = basePath is null ? "null" : $"\"{EscapeStringLiteral(basePath)}\"";
            var relPathLiteral = $"\"{EscapeStringLiteral(relativePath)}\"";
            writer.WriteLine($"var _builder = new global::CoreOne.Http.RestRequestBuilder(\"{EscapeStringLiteral(httpMethod)}\", {basePathLiteral}, {relPathLiteral});");

            if (allStaticHeaders.Count > 0)
            {
                var headerLiterals = string.Join(", ", allStaticHeaders.Select(h => $"\"{EscapeStringLiteral(h)}\""));
                writer.WriteLine($"_builder.AddHeaders({headerLiterals});");
            }

            if (isMultipart)
            {
                writer.WriteLine($"_builder.AsMultipart(\"{EscapeStringLiteral(multipartBoundary)}\");");
            }

            var combinedRoute = $"{basePath}/{relativePath}";
            string cancellationTokenExpr = "global::System.Threading.CancellationToken.None";
            bool hasExplicitBody = method.Parameters.Any(p => HasAttribute(p, BodyAttributeName));

            foreach (var param in method.Parameters)
            {
                var paramTypeName = param.Type.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString();
                if (paramTypeName == "System.Threading.CancellationToken")
                {
                    cancellationTokenExpr = param.Name;
                    continue;
                }

                var alias = GetAliasName(param);
                var effectiveName = alias ?? param.Name;

                if (TryGetHeaderAttribute(param, out var headerName))
                {
                    writer.WriteLine($"_builder.AddHeader(\"{EscapeStringLiteral(headerName)}\", {param.Name});");
                    continue;
                }

                if (TryGetAuthorizeAttribute(param, out var authScheme))
                {
                    writer.WriteLine($"_builder.WithAuthorization(\"{EscapeStringLiteral(authScheme)}\", {param.Name});");
                    continue;
                }

                if (TryGetBodyAttribute(param, out var bodySerializationMethod, out var bodySerializerKey))
                {
                    var bodySerializerExpr = serializerVar;
                    if (!string.IsNullOrEmpty(bodySerializerKey))
                    {
                        bodySerializerExpr = $"global::CoreOne.Http.Extensions.SerializerHttpExtensions.ResolveSerializer(Services, \"{EscapeStringLiteral(bodySerializerKey!)}\", null, {serializerVar})";
                    }
                    var modeEnum = bodySerializationMethod == 1
                        ? "global::CoreOne.Http.Attributes.BodySerializationMethod.UrlEncoded"
                        : "global::CoreOne.Http.Attributes.BodySerializationMethod.Serialized";
                    writer.WriteLine($"_builder.WithBody({param.Name}, {bodySerializerExpr}, {modeEnum});");
                    continue;
                }

                if (TryGetQueryParamAttribute(param, out var queryAlias, out var queryFormat))
                {
                    var qName = !string.IsNullOrEmpty(queryAlias) ? queryAlias! : effectiveName;
                    var fmtLiteral = string.IsNullOrEmpty(queryFormat) ? "null" : $"\"{EscapeStringLiteral(queryFormat!)}\"";
                    writer.WriteLine($"_builder.AddQueryParam(\"{EscapeStringLiteral(qName)}\", {param.Name}, {fmtLiteral});");
                    continue;
                }

                if (ContainsRouteToken(combinedRoute, effectiveName))
                {
                    writer.WriteLine($"_builder.AddRouteParam(\"{EscapeStringLiteral(effectiveName)}\", {param.Name});");
                    continue;
                }

                if (ContainsRouteToken(combinedRoute, param.Name))
                {
                    writer.WriteLine($"_builder.AddRouteParam(\"{EscapeStringLiteral(param.Name)}\", {param.Name});");
                    continue;
                }

                if (isMultipart || IsMultipartItemType(param.Type))
                {
                    writer.WriteLine($"_builder.AddMultipartItem(\"{EscapeStringLiteral(effectiveName)}\", {param.Name}, {serializerVar});");
                    continue;
                }

                if (paramTypeName == "CoreOne.ODataBuilders.ODataBuilder")
                {
                    writer.WriteLine($"_builder.AddQueryParam(\"{EscapeStringLiteral(effectiveName)}\", {param.Name});");
                    continue;
                }

                if (!hasExplicitBody && IsBodyCapableVerb(httpMethod) && !IsSimpleQueryType(param.Type))
                {
                    hasExplicitBody = true;
                    writer.WriteLine($"_builder.WithBody({param.Name}, {serializerVar}, global::CoreOne.Http.Attributes.BodySerializationMethod.Serialized);");
                    continue;
                }

                writer.WriteLine($"_builder.AddQueryParam(\"{EscapeStringLiteral(effectiveName)}\", {param.Name});");
            }

            writer.WriteLine("var _request = _builder.Build();");
            WriteReturnInvocation(writer, method.ReturnType, serializerVar, cancellationTokenExpr);
        }
    }

    private static void GenerateRestClient(SourceProductionContext context, INamedTypeSymbol interfaceSymbol)
    {
        var namespaceName = interfaceSymbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : interfaceSymbol.ContainingNamespace.ToDisplayString();

        var (basePath, customClientName, restClientSerializerKey) = GetRestClientAttributeValues(interfaceSymbol);
        var (attrSerializerKey, attrSerializerType) = ExtractSerializerInfo(interfaceSymbol);
        var defaultSerializerKey = attrSerializerKey ?? restClientSerializerKey;
        var defaultSerializerType = attrSerializerType;

        var clientClassName = !string.IsNullOrWhiteSpace(customClientName)
            ? customClientName!
            : GetDefaultClientClassName(interfaceSymbol.Name);

        var allMethods = GetAllInterfaceMethods(interfaceSymbol);
        var restMethods = new List<(IMethodSymbol Method, string HttpMethod, string RelativePath)>();

        foreach (var method in allMethods)
        {
            if (TryGetHttpMethodAttribute(method, out var verb, out var relPath))
            {
                restMethods.Add((method, verb, relPath));
            }
        }

        if (restMethods.Count == 0 && !HasAttribute(interfaceSymbol, RestClientAttributeName))
            return;

        var interfaceHeaders = ExtractStaticHeaders(interfaceSymbol);
        var interfaceInterceptors = ProxyGenerator.GetInterceptorTypes(interfaceSymbol);

        var writer = new CSharpFileWriter();
        writer.WriteLine("// <auto-generated/>");
        writer.WriteLine("#nullable enable");
        writer.WriteLine("using CoreOne;");
        writer.WriteLine("using System;");
        writer.WriteLine("using System.Net.Http;");
        writer.WriteLine("using System.Threading;");
        writer.WriteLine("using System.Threading.Tasks;");
        writer.WriteLine();

        var namespaceBlock = namespaceName is not null ? writer.BeginBlock($"namespace {namespaceName}") : null;

        writer.WriteLine($"[global::CoreOne.Attributes.ServiceAttribute<{interfaceSymbol.ToDisplayString()}>(global::Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped)]");
        foreach (var interceptorType in interfaceInterceptors)
        {
            writer.WriteLine($"[global::CoreOne.Attributes.InterceptAttribute(typeof({interceptorType}))]");
        }

        using (writer.BeginBlock($"public partial class {clientClassName} : {interfaceSymbol.ToDisplayString()}"))
        {
            writer.WriteLine("protected global::System.Net.Http.HttpClient HttpClient { get; }");
            writer.WriteLine("protected global::System.IServiceProvider? Services { get; }");
            writer.WriteLine("protected global::CoreOne.ISerializer Serializer { get; }");
            writer.WriteLine();

            using (writer.BeginBlock($"public {clientClassName}(global::System.Net.Http.HttpClient httpClient, global::System.IServiceProvider? services = null, global::CoreOne.ISerializer? serializer = null)"))
            {
                writer.WriteLine("HttpClient = httpClient ?? throw new global::System.ArgumentNullException(nameof(httpClient));");
                writer.WriteLine("Services = services;");

                var keyLiteral = string.IsNullOrEmpty(defaultSerializerKey) ? "null" : $"\"{EscapeStringLiteral(defaultSerializerKey!)}\"";
                var typeLiteral = string.IsNullOrEmpty(defaultSerializerType) ? "null" : $"typeof({defaultSerializerType})";
                writer.WriteLine($"Serializer = serializer ?? global::CoreOne.Http.Extensions.SerializerHttpExtensions.ResolveSerializer(services, {keyLiteral}, {typeLiteral}, null);");
            }

            foreach (var (method, verb, relPath) in restMethods)
            {
                GenerateMethodImplementation(writer, method, basePath, interfaceHeaders, verb, relPath);
            }
        }

        namespaceBlock?.Dispose();

        context.AddSource($"{clientClassName}.g.cs", writer.ToSourceText());

        // Stage 2: Emit {ClientName}Proxy.g.cs when [Intercept] is present on interface or methods
        ProxyGenerator.GenerateProxyForClient(
            context,
            interfaceSymbol,
            clientClassName,
            restMethods.Select(m => m.Method).ToList(),
            namespaceName);
    }

    private static string FormatParameter(IParameterSymbol param)
    {
        var typeStr = param.Type.ToDisplayString();
        if (!param.HasExplicitDefaultValue)
            return $"{typeStr} {param.Name}";

        var defaultVal = param.ExplicitDefaultValue;
        string defaultExpr = defaultVal switch {
            null => "default!",
            bool b => b ? "true" : "false",
            string s => $"\"{EscapeStringLiteral(s)}\"",
            char c => $"'{c}'",
            IFormattable f => f.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
            _ => "default"
        };

        if (defaultVal is null && (param.Type.IsValueType || param.NullableAnnotation == NullableAnnotation.Annotated))
            defaultExpr = "default";

        return $"{typeStr} {param.Name} = {defaultExpr}";
    }

    private static string? GetAliasName(ISymbol symbol)
    {
        foreach (var attr in symbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == AliasAsAttributeName &&
                attr.ConstructorArguments.Length > 0 &&
                attr.ConstructorArguments[0].Value is string name &&
                !string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
        }
        return null;
    }

    private static List<IMethodSymbol> GetAllInterfaceMethods(INamedTypeSymbol interfaceSymbol)
    {
        var methods = new List<IMethodSymbol>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var m in interfaceSymbol.GetMembers().OfType<IMethodSymbol>().Where(m => m.MethodKind == MethodKind.Ordinary))
        {
            methods.Add(m);
            seen.Add(ProxyGenerator.GetSafeMethodName(m));
        }

        foreach (var baseIface in interfaceSymbol.AllInterfaces)
        {
            foreach (var m in baseIface.GetMembers().OfType<IMethodSymbol>().Where(m => m.MethodKind == MethodKind.Ordinary))
            {
                if (seen.Add(ProxyGenerator.GetSafeMethodName(m)))
                    methods.Add(m);
            }
        }

        return methods;
    }

    private static (string? BasePath, string? ClientName, string? SerializerKey) GetRestClientAttributeValues(INamedTypeSymbol interfaceSymbol)
    {
        foreach (var attr in interfaceSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == RestClientAttributeName)
            {
                string? basePath = attr.ConstructorArguments.Length > 0 ? attr.ConstructorArguments[0].Value as string : null;
                string? clientName = attr.ConstructorArguments.Length > 1 ? attr.ConstructorArguments[1].Value as string : null;
                string? serializerKey = attr.ConstructorArguments.Length > 2 ? attr.ConstructorArguments[2].Value as string : null;
                return (basePath, clientName, serializerKey);
            }
        }
        return (null, null, null);
    }

    private static InterfaceDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var interfaceDeclaration = (InterfaceDeclarationSyntax)context.Node;

        foreach (var attributeList in interfaceDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                if (context.SemanticModel.GetSymbolInfo(attribute).Symbol is IMethodSymbol attributeSymbol)
                {
                    var fullName = attributeSymbol.ContainingType.ToDisplayString();
                    if (fullName == RestClientAttributeName)
                        return interfaceDeclaration;
                }
            }
        }

        foreach (var member in interfaceDeclaration.Members.OfType<MethodDeclarationSyntax>())
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
                        if (HttpVerbMap.Contains(fullName) || baseFullName == HttpMethodAttributeName || fullName == HttpMethodAttributeName)
                            return interfaceDeclaration;
                    }
                }
            }
        }

        return null;
    }

    private static bool HasAttribute(ISymbol symbol, string attributeFullName) =>
        symbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == attributeFullName);

    private static bool IsBodyCapableVerb(string verb) =>
        verb.Equals("POST", StringComparison.OrdinalIgnoreCase) ||
        verb.Equals("PUT", StringComparison.OrdinalIgnoreCase) ||
        verb.Equals("PATCH", StringComparison.OrdinalIgnoreCase) ||
        verb.Equals("QUERY", StringComparison.OrdinalIgnoreCase);

    private static bool IsMultipartItemType(ITypeSymbol type)
    {
        var current = type;
        while (current is not null)
        {
            if (current.ToDisplayString() == "CoreOne.Http.Models.MultipartItem")
                return true;
            current = current.BaseType;
        }

        if (type is INamedTypeSymbol named && named.IsGenericType && named.TypeArguments.Length == 1)
        {
            var arg = named.TypeArguments[0];
            while (arg is not null)
            {
                if (arg.ToDisplayString() == "CoreOne.Http.Models.MultipartItem")
                    return true;
                arg = arg.BaseType;
            }
        }

        return false;
    }

    private static bool IsSimpleQueryType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol named && named.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
            type = named.TypeArguments[0];

        if (type.TypeKind == TypeKind.Enum)
            return true;

        switch (type.SpecialType)
        {
            case SpecialType.System_Boolean:
            case SpecialType.System_Byte:
            case SpecialType.System_SByte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_Decimal:
            case SpecialType.System_Char:
            case SpecialType.System_String:
            case SpecialType.System_DateTime:
                return true;
        }

        var display = type.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString();
        return display is "System.Guid" or "System.DateTimeOffset" or "System.TimeSpan" or "System.Uri" or "CoreOne.ID";
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        if (node is not InterfaceDeclarationSyntax interfaceDeclaration)
            return false;
        if (interfaceDeclaration.AttributeLists.Count > 0)
            return true;
        return interfaceDeclaration.Members
            .OfType<MethodDeclarationSyntax>()
            .Any(m => m.AttributeLists.Count > 0);
    }

    private static bool TryGetAuthorizeAttribute(IParameterSymbol param, out string scheme)
    {
        foreach (var attr in param.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == AuthorizeAttributeName)
            {
                scheme = attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string s && !string.IsNullOrWhiteSpace(s)
                    ? s
                    : "Bearer";
                return true;
            }
        }
        scheme = string.Empty;
        return false;
    }

    private static bool TryGetBodyAttribute(IParameterSymbol param, out int serializationMethod, out string? serializerKey)
    {
        foreach (var attr in param.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == BodyAttributeName)
            {
                serializationMethod = attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is int m ? m : 0;
                serializerKey = attr.ConstructorArguments.Length > 1 ? attr.ConstructorArguments[1].Value as string : null;
                return true;
            }
        }
        serializationMethod = 0;
        serializerKey = null;
        return false;
    }

    private static bool TryGetHeaderAttribute(IParameterSymbol param, out string headerName)
    {
        foreach (var attr in param.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == HeaderAttributeName &&
                attr.ConstructorArguments.Length > 0 &&
                attr.ConstructorArguments[0].Value is string h &&
                !string.IsNullOrWhiteSpace(h))
            {
                headerName = h;
                return true;
            }
        }
        headerName = string.Empty;
        return false;
    }

    private static bool TryGetHttpMethodAttribute(IMethodSymbol method, out string verb, out string relativePath)
    {
        foreach (var attr in method.GetAttributes())
        {
            var attrClass = attr.AttributeClass;
            if (attrClass is null)
                continue;

            var fullName = attrClass.ToDisplayString();
            var baseFullName = attrClass.BaseType?.ToDisplayString();
            if (HttpVerbMap.Contains(fullName) || baseFullName == HttpMethodAttributeName || fullName == HttpMethodAttributeName)
            {
                if (fullName == HttpMethodAttributeName)
                {
                    verb = attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string v ? v : "GET";
                    relativePath = attr.ConstructorArguments.Length > 1 && attr.ConstructorArguments[1].Value is string p ? p : string.Empty;
                    return true;
                }

                verb = fullName switch {
                    "CoreOne.Http.Attributes.GetAttribute" => "GET",
                    "CoreOne.Http.Attributes.PostAttribute" => "POST",
                    "CoreOne.Http.Attributes.PutAttribute" => "PUT",
                    "CoreOne.Http.Attributes.DeleteAttribute" => "DELETE",
                    "CoreOne.Http.Attributes.PatchAttribute" => "PATCH",
                    "CoreOne.Http.Attributes.HeadAttribute" => "HEAD",
                    "CoreOne.Http.Attributes.OptionsAttribute" => "OPTIONS",
                    "CoreOne.Http.Attributes.QueryAttribute" => "QUERY",
                    _ => "GET"
                };
                relativePath = attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string path ? path : string.Empty;
                return true;
            }
        }

        verb = string.Empty;
        relativePath = string.Empty;
        return false;
    }

    private static bool TryGetQueryParamAttribute(IParameterSymbol param, out string? alias, out string? format)
    {
        foreach (var attr in param.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == QueryParamAttributeName)
            {
                alias = attr.ConstructorArguments.Length > 0 ? attr.ConstructorArguments[0].Value as string : null;
                format = attr.ConstructorArguments.Length > 1 ? attr.ConstructorArguments[1].Value as string : null;
                return true;
            }
        }
        alias = null;
        format = null;
        return false;
    }

    private static void WriteReturnInvocation(
        CSharpFileWriter writer,
        ITypeSymbol returnTypeSymbol,
        string serializerVar,
        string cancellationTokenExpr)
    {
        var fullReturn = returnTypeSymbol.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString();
        if (fullReturn == "System.Threading.Tasks.Task")
        {
            writer.WriteLine($"await global::CoreOne.Http.RestClientInvoker.SendAsync(HttpClient, _request, {cancellationTokenExpr}).ConfigureAwait(false);");
            return;
        }

        if (returnTypeSymbol is INamedTypeSymbol taskType && taskType.IsGenericType && taskType.TypeArguments.Length == 1)
        {
            var innerSymbol = taskType.TypeArguments[0];
            var innerNoNull = innerSymbol.WithNullableAnnotation(NullableAnnotation.None);
            var innerDisplay = innerNoNull.ToDisplayString();

            if (innerDisplay == "CoreOne.Results.HttpResult")
            {
                writer.WriteLine($"return await global::CoreOne.Http.RestClientInvoker.SendForHttpResultAsync(HttpClient, _request, {cancellationTokenExpr}).ConfigureAwait(false);");
                return;
            }

            if (innerDisplay == "CoreOne.Results.IResult")
            {
                writer.WriteLine($"return await global::CoreOne.Http.RestClientInvoker.SendForIResultAsync(HttpClient, _request, {cancellationTokenExpr}).ConfigureAwait(false);");
                return;
            }

            if (innerDisplay == "CoreOne.Results.Result")
            {
                writer.WriteLine($"return await global::CoreOne.Http.RestClientInvoker.SendForResultAsync(HttpClient, _request, {cancellationTokenExpr}).ConfigureAwait(false);");
                return;
            }

            if (innerNoNull is INamedTypeSymbol genericInner && genericInner.IsGenericType)
            {
                var openGeneric = genericInner.ConstructedFrom.ToDisplayString();
                if (openGeneric == "CoreOne.Results.HttpResult<T>")
                {
                    var modelType = genericInner.TypeArguments[0].ToDisplayString();
                    writer.WriteLine($"return await global::CoreOne.Http.RestClientInvoker.SendForHttpResultAsync<{modelType}>(HttpClient, _request, {serializerVar}, {cancellationTokenExpr}).ConfigureAwait(false);");
                    return;
                }

                if (openGeneric == "CoreOne.Results.HttpResult<TModel, TError>")
                {
                    var modelType = genericInner.TypeArguments[0].ToDisplayString();
                    var errorType = genericInner.TypeArguments[1].ToDisplayString();
                    writer.WriteLine($"return await global::CoreOne.Http.RestClientInvoker.SendForHttpResultAsync<{modelType}, {errorType}>(HttpClient, _request, {serializerVar}, {cancellationTokenExpr}).ConfigureAwait(false);");
                    return;
                }

                if (openGeneric == "CoreOne.Results.IResult<TModel>")
                {
                    var modelType = genericInner.TypeArguments[0].ToDisplayString();
                    writer.WriteLine($"return await global::CoreOne.Http.RestClientInvoker.SendForIResultAsync<{modelType}>(HttpClient, _request, {serializerVar}, {cancellationTokenExpr}).ConfigureAwait(false);");
                    return;
                }

                if (openGeneric == "CoreOne.Results.Result<T>")
                {
                    var modelType = genericInner.TypeArguments[0].ToDisplayString();
                    writer.WriteLine($"return await global::CoreOne.Http.RestClientInvoker.SendForResultAsync<{modelType}>(HttpClient, _request, {serializerVar}, {cancellationTokenExpr}).ConfigureAwait(false);");
                    return;
                }
            }

            var targetModelType = innerSymbol.ToDisplayString();
            writer.WriteLine($"return (await global::CoreOne.Http.RestClientInvoker.SendForModelAsync<{targetModelType}>(HttpClient, _request, {serializerVar}, {cancellationTokenExpr}).ConfigureAwait(false))!;");
        }
    }
}
