using System.Text;
using System.Text.RegularExpressions;

namespace OneCore.IO;

public static partial class IOUtility
{
    private static readonly Regex InvalidChars = InvalidCharRegex();
    private static readonly Regex MultipleHyphens = MultipleHyphenRegex();
    private static readonly Regex WordDelimiters = WordDelimiterRegex();
    public static char[] Invalid { get; private set; }

    static IOUtility()
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
    public static string CheckDirectory(string path)
    {
        if (!string.IsNullOrWhiteSpace(path) && !Directory.Exists(path))
        {
            Utility.Try(() => Directory.CreateDirectory(path));
        }

        return path;
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

    public static async ValueTask LoadFromResource(Assembly? assembly, string filename, Func<Stream, Task> callback)
    {
        if (assembly is null)
            return;
        try
        {
            var names = assembly.GetManifestResourceNames();
            var target = names.FirstOrDefault(p => p.Matches(filename));
            target ??= names.FirstOrDefault(p => p.EndsWith(filename, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(target))
            {
                var stream = assembly.GetManifestResourceStream(target);
                if (stream != null)
                {
                    await callback.Invoke(stream);
                }
            }
        }
        catch { }
    }

    /// <summary>
    /// Reads all bytes from file
    /// </summary>
    /// <param name="path"></param>
    /// <param name="mode"></param>
    /// <param name="access"></param>
    /// <param name="share"></param>
    /// <returns></returns>
    public static byte[]? ReadAllBytes(string path, FileMode mode = FileMode.Open, FileAccess access = FileAccess.Read, FileShare share = FileShare.Read)
    {
        byte[]? buffer = null;
        if (!string.IsNullOrEmpty(path))
        {
            try
            {
                using (var stream = new FileStream(path, mode, access, share))
                    buffer = stream.ReadFully();

                File.SetLastAccessTime(path, DateTime.Now);
            }
            catch
            {
            }
        }

        return buffer;
    }

    /// <summary>
    /// Remove invalid chars from path
    /// </summary>
    /// <param name="path">File path</param>
    /// <returns>Win-safe path</returns>
    public static string RemoveInvalid(string path) => path.Remove(Invalid);

    public static string ToSlug(string value)
    {
        // convert to lower case
        value = value.ToLowerInvariant();

        // remove diacritics (accents)
        value = RemoveDiacritics(value);

        // ensure all word delimiters are hyphens
        value = WordDelimiters.Replace(value, "-");

        // strip out invalid characters
        value = InvalidChars.Replace(value, "");

        // replace multiple hyphens (-) with a single hyphen
        value = MultipleHyphens.Replace(value, "-");

        // trim hyphens (-) from ends
        return value.Trim('-');

        /// See: http://www.siao2.com/2007/05/14/2629747.aspx
        static string RemoveDiacritics(string stIn)
        {
            var formD = stIn.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            for (int ich = 0; ich < formD.Length; ich++)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(formD[ich]);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(formD[ich]);
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }

    /// <summary>
    /// Writes all bytes to file
    /// </summary>
    /// <param name="path"></param>
    /// <param name="buffer"></param>
    /// <param name="mode"></param>
    /// <param name="access"></param>
    /// <param name="share"></param>
    /// <returns></returns>
    public static IResult<bool> WriteAllBytes(string path, byte[] buffer, FileMode mode = FileMode.OpenOrCreate, FileAccess access = FileAccess.ReadWrite, FileShare share = FileShare.ReadWrite)
    {
        return Utility.Try(() => {
            using var stream = new FileStream(path, mode, access, share);
            stream.Append(buffer);
            return true;
        });
    }

    [GeneratedRegex(@"[^a-z0-9\-]", RegexOptions.Compiled)]
    private static partial Regex InvalidCharRegex();

    [GeneratedRegex(@"-{2,}", RegexOptions.Compiled)]
    private static partial Regex MultipleHyphenRegex();

    [GeneratedRegex(@"[\s—–_]", RegexOptions.Compiled)]
    private static partial Regex WordDelimiterRegex();
}