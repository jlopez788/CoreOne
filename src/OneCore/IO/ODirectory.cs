namespace OneCore.IO;

public sealed class ODirectory : IDisposable
{
    public string Folder { get; }
    internal SToken Token { get; }

    public ODirectory(string folder)
    {
        Folder = folder;
        Token = SToken.Create();
    }

    private ODirectory(ODirectory directory, string path)
    {
        Token = directory.Token;
        Folder = Path.Combine(directory.Folder, path);
    }

    public static ODirectory FromApplicationName(string? applicationName = null, Environment.SpecialFolder rootFolder = Environment.SpecialFolder.UserProfile)
    {
        var path = Environment.GetFolderPath(rootFolder);
        return new ODirectory(Path.Combine(path, applicationName ?? "one"));
    }

    public static ODirectory FromDirectoryPath(string path) => new(path);

    public static implicit operator string(ODirectory directory) => directory.Folder;

    public ODirectory ChildDirectory(string folder) => new(this, folder);

    public void Dispose()
    {
        Token.Dispose();
        GC.SuppressFinalize(this);
    }

    public bool Exists() => Directory.Exists(Folder);

    public OFile File(string name, bool isTemporary)
    {
        var directory = isTemporary ? ChildDirectory("temp") : this;
        var file = new OFile(directory, IOUtility.RemoveInvalid(name));
        if (isTemporary)
            Token.Register(file);

        IOUtility.CheckDirectory(directory);
        return file;
    }

    public OFile Random(string ext, bool isTemporary)
    {
        ext = ext.TrimStart('.');
        var name = $"{IOUtility.ToSlug(ID.Create().ToSlugUrl())}.{ext}";
        return File(name, isTemporary);
    }

    public ODirectory Verify()
    {
        IOUtility.CheckDirectory(Folder);
        return this;
    }
}