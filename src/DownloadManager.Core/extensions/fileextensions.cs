namespace DownloadManager.Core.Ext;
internal static class FileExt
// .ext list that will be moved to fitting folder
{
    private static readonly Dictionary<string, string> Categories =
    new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "Image",
        [".jpg"] = "Image",
        [".jpeg"] = "Image",
        [".gif"] = "Image",
        [".webp"] = "Image",
        [".mp4"] = "Video",
        [".avi"] = "Video",
        [".mov"] = "Video",
        [".mkv"] = "Video",
        [".wmv"] = "Video",
        [".xls"] = "Document",
        [".doc"] = "Document",
        [".docx"] = "Document",
        [".pdf"] = "Document",
        [".xlsx"] = "Document",
        [".mp3"] = "Music",
        [".wav"] = "Music",
        [".flac"] = "Music",
        [".aac"] = "Music",
        [".m4a"] = "Music",
        [".ogg"] = "Music",
        [".wma"] = "Music",
        [".aiff"] = "Music",
        [".alac"] = "Music",
        [".opus"] = "Music"
    };
    internal static string GetCategory(string extension)
    {
        if (Categories.TryGetValue(extension, out string? category))
        {
            return category;
        }

        return "Unknown";
    }
    internal static bool IsTemporaryFile(string path)
{
    string extension = Path.GetExtension(path);
    // ignoring .ext
    return extension.Equals(".tmp", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".crdownload", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".part", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".download", StringComparison.OrdinalIgnoreCase);
}
}