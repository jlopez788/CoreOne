using System.Text;

namespace OneCore.IO;

public sealed class OFile(string path, string name) : IDisposable
{
    private string? BFilename;
    public ODirectory? Directory { get; }
    public string FilePath => BFilename ??= System.IO.Path.Combine(Path, Name);
    public string Name { get; } = name;
    public string Path { get; } = path;

    public OFile(ODirectory directory, string name) : this(directory.Folder, name)
    {
        Directory = directory;
    }

    public static implicit operator string(OFile file) => file.FilePath;

    public void Dispose()
    {
        Utility.Try(() => File.Delete(FilePath));
        GC.SuppressFinalize(this);
    }

    public bool Exists() => File.Exists(FilePath);

    public StreamWriter OpenStreamWriter()
    {
        IOUtility.CheckDirectory(Path);
        return new(FilePath, false, Encoding.UTF8);
    }
}