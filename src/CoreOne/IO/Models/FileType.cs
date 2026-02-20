using System;
using System.Collections.Generic;
using System.Text;

namespace CoreOne.IO.Models;

public class FileType(string? ext, string? mimeType) : IEquatable<FileType>
{
    public static readonly FileType
        EMPTY = new(),
        PDF = new(".pdf", "application/pdf"),
        ZIP = new(".zip", "application/zip"),
        JSON = new(".json", "application/json"),
        XLSX = new(".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

    public string Extension { get; } = ext ?? string.Empty;
    public string MimeType { get; } = mimeType ?? string.Empty;

    public FileType() : this(null, null) { }

    public static bool operator !=(FileType? left, FileType? right) => !(left == right);

    public static bool operator ==(FileType? left, FileType? right) => (left is null && right is null) || left?.Equals(right) == true;

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is FileType fileType && Equals(fileType);

    public bool Equals(FileType? other) => Extension.Matches(other?.Extension) && MimeType.Matches(other?.MimeType);

    public override int GetHashCode() => (Extension, MimeType).GetHashCode();

    public string GetRandomName() => $"{ID.Create().ToShortId()}{Extension}";
}