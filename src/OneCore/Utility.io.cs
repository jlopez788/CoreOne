namespace OneCore;

public static partial class Utility
{
    public static char[] Invalid { get; private set; }

    static Utility()
    {
        var invalid = new List<char>(15);
        invalid.AddRange(Path.GetInvalidFileNameChars());
        invalid.AddRange(Path.GetInvalidPathChars());
        invalid.Sort();
        Invalid = [.. invalid];
    }

    /// <summary>
    /// Checks to see if directory exists, creates it if it doesn't
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static IResult<string> CheckDirectory(string path)
    {
        if (!string.IsNullOrWhiteSpace(path) && !Directory.Exists(path))
        {
            path = path.Remove(Invalid);
            try
            { Directory.CreateDirectory(path); }
            catch (Exception ex) { return Result.FromException<string>(ex); }
        }

        return new Result<string>(path);
    }

    /// <summary> Fast file copy with big buffers
    /// </summary>
    /// <param name="source">Source file path</param>
    /// <param name="destination">Destination file path</param>
    public static Result FCopy(string source, string destination)
    {
        int dataLength = (int)Math.Pow(2, 19);
        byte[] buffer = new byte[dataLength];
        var result = new Result(ResultType.Success, "");
        try
        {
            using var fsread = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.None, dataLength);
            using var fswrite = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, dataLength);
            int read;
            while ((read = fsread.Read(buffer, 0, dataLength)) > 0)
            {
                fswrite.Write(buffer, 0, read);
            }
        }
        catch (Exception ex)
        {
            result = new Result(ResultType.Exception, ex.Message);
        }

        return result;
    }

    /// <summary>
    /// Remove invalid chars from path
    /// </summary>
    /// <param name="path">File path</param>
    /// <returns>Win-safe path</returns>
    public static string RemoveInvalid(string path) => path.Remove(Invalid);
}