namespace CoreOne.Generators.StronglyTypedIds;

internal sealed class CSharpFileWriter
{
    private sealed class CloseBlock(CSharpFileWriter writer, int count) : IDisposable
    {
        public void Dispose()
        {
            for (var i = 0; i < count; i++)
            {
                writer.EndBlock();
            }
        }
    }

    private readonly StringBuilder _builder = new(capacity: 2000);
    private bool _isEndOfBlock;
    private bool _mustIndent = true;
    public int Indentation { get; set; }
    public string IndentationString { get; set; } = "\t";

    public IDisposable BeginBlock(string value)
    {
        WriteLine(value);
        WriteLine('{');
        Indentation++;
        return new CloseBlock(this, 1);
    }

    public IDisposable BeginBlock()
    {
        WriteLine('{');
        Indentation++;
        return new CloseBlock(this, 1);
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "")]
    public IDisposable BeginPartialContext(ITypeSymbol type, Action<CSharpFileWriter>? writeAttributes = null, string? baseTypes = null)
    {
        var initialIndentation = Indentation;
        var ns = GetNamespace(type.ContainingNamespace);
        if (ns is not null)
        {
            WriteLine("namespace " + ns);
            BeginBlock();
        }

        WriteContainingTypes(type.ContainingType);
        writeAttributes?.Invoke(this);
        WriteBeginType(type, baseTypes);
        return new CloseBlock(this, Indentation - initialIndentation);

        void WriteContainingTypes(ITypeSymbol? containingType)
        {
            if (containingType is null)
                return;

            WriteContainingTypes(containingType.ContainingType);
            WriteBeginType(containingType, baseTypes: null);
        }

        void WriteBeginType(ITypeSymbol typeSymbol, string? baseTypes)
        {
            var text = typeSymbol switch {
                { IsValueType: false, IsRecord: false } => "partial class " + typeSymbol.Name,
                { IsValueType: false, IsRecord: true } => "partial record " + typeSymbol.Name,
                { IsValueType: true, IsRecord: false } => "partial struct " + typeSymbol.Name,
                { IsValueType: true, IsRecord: true } => "partial record struct " + typeSymbol.Name,
            };

            Write(text);
            if (baseTypes is not null)
            {
                Write(" : ");
                Write(baseTypes);
            }

            WriteLine();
            _ = BeginBlock();
        }

        static string? GetNamespace(INamespaceSymbol ns)
        {
            string? str = null;
            while (ns is not null && !ns.IsGlobalNamespace)
            {
                if (str is not null)
                {
                    str = '.' + str;
                }

                str = ns.Name + str;
                ns = ns.ContainingNamespace;
            }

            return str;
        }
    }

    public void EndBlock()
    {
        Indentation--;
        WriteLine('}');
    }

    public void EnsureFreeCapacity(int length)
    {
        _builder.EnsureCapacity(_builder.Capacity + length);
    }

    public SourceText ToSourceText() => SourceText.From(_builder.ToString(), Encoding.UTF8);

    public void Write(char text)
    {
        WriteIndentation();
        _builder.Append(text);
    }

    public void Write(string text)
    {
        WriteIndentation();
        _builder.Append(text);
    }

    public void WriteAccessibility(Accessibility accessibility)
    {
        switch (accessibility)
        {
            case Accessibility.Private:
                Write("private");
                break;

            case Accessibility.ProtectedAndInternal:
                Write("private protected");
                break;

            case Accessibility.Protected:
                Write("protected");
                break;

            case Accessibility.Internal:
                Write("internal");
                break;

            case Accessibility.ProtectedOrInternal:
                Write("protected internal");
                break;

            case Accessibility.Public:
                Write("public");
                break;
        }
    }

    public void WriteLine()
    {
        _builder.Append('\n');
        _mustIndent = true;
        _isEndOfBlock = false;
    }

    public void WriteLine(string text)
    {
        if (_isEndOfBlock)
        {
            _isEndOfBlock = false;
            if (text is not "}" and not "else")
            {
                WriteLine();
            }
        }

        WriteIndentation();
        EnsureFreeCapacity(text.Length + 1);
        _builder.Append(text);
        _builder.Append('\n');
        _mustIndent = true;
        _isEndOfBlock = text == "}";
    }

    public void WriteLine(char text)
    {
        if (_isEndOfBlock)
        {
            _isEndOfBlock = false;
            if (text != '}')
            {
                WriteLine();
            }
        }

        WriteIndentation();
        EnsureFreeCapacity(2);
        _builder.Append(text);
        _builder.Append('\n');
        _mustIndent = true;
        _isEndOfBlock = text == '}';
    }

    public void WriteXmlComment(XNode[] nodes)
    {
        foreach (var node in nodes)
        {
            WriteXmlComment(node);
        }
    }

    public void WriteXmlComment(XNode node)
    {
        var content = node.ToString();
        using var reader = new StringReader(content);
        while (reader.ReadLine() is string line)
        {
            Write("/// ");
            WriteLine(line);
        }
    }

    private void WriteIndentation()
    {
        if (!_mustIndent)
            return;

        for (var i = 0; i < Indentation; i++)
        {
            _builder.Append(IndentationString);
        }

        _mustIndent = false;
    }
}