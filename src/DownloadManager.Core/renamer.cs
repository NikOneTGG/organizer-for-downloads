namespace DownloadManager.Core;

internal sealed record RenameSettings(
    string Nameefile,
    bool Dateandtime,
    bool Randomnum);

internal static class Erename
{
    private static readonly string ConfigPath =
        Path.Combine(AppContext.BaseDirectory, "config.json");

    private static readonly object SettingsLock = new();
    private static RenameSettings? _cachedSettings;
    private static DateTime _cachedWriteTimeUtc;

    private static RenameSettings GetSettings()
    {
        DateTime writeTime = File.Exists(ConfigPath)
            ? File.GetLastWriteTimeUtc(ConfigPath)
            : DateTime.MinValue;

        lock (SettingsLock)
        {
            if (_cachedSettings is null || writeTime != _cachedWriteTimeUtc)
            {
                AppSettings settings = AppSettings.LoadOrCreate(ConfigPath);
                _cachedSettings = new RenameSettings(
                    settings.Namefile,
                    settings.Dateandtime,
                    settings.Randomnum);

                _cachedWriteTimeUtc = File.Exists(ConfigPath)
                    ? File.GetLastWriteTimeUtc(ConfigPath)
                    : DateTime.MinValue;
            }

            return _cachedSettings;
        }
    }

    internal static bool IsEnabled
    {
        get
        {
            RenameSettings settings = GetSettings();
            return !string.IsNullOrWhiteSpace(settings.Nameefile)
                || settings.Dateandtime
                || settings.Randomnum;
        }
    }

    internal static string GetFileName(string sourcePath, string destinationFolder) =>
        GetFileName(sourcePath, destinationFolder, DateTime.Now, GetSettings());

    internal static string GetFileName(
        string sourcePath,
        string destinationFolder,
        DateTime dateTime,
        RenameSettings settings)
    {
        string extension = Path.GetExtension(sourcePath);
        string originalName = Path.GetFileNameWithoutExtension(sourcePath);
        string baseName = string.IsNullOrWhiteSpace(settings.Nameefile)
            ? originalName
            : settings.Nameefile;

        if (settings.Dateandtime)
            baseName = $"{baseName}_{dateTime:dd.MM.yyyy_HHmm}";

        Span<int> numbers = stackalloc int[50];
        int count = settings.Randomnum ? numbers.Length : 1;

        if (settings.Randomnum)
        {
            for (int i = 0; i < numbers.Length; i++)
                numbers[i] = i + 1;

            Random.Shared.Shuffle(numbers);
        }
        else
        {
            numbers[0] = 0;
        }

        foreach (int number in numbers[..count])
        {
            string suffix = settings.Randomnum ? $"_{number}" : "";
            string fileName = baseName + suffix + extension;
            string destinationPath = Path.Combine(destinationFolder, fileName);

            if (!File.Exists(destinationPath) && !Directory.Exists(destinationPath))
                return fileName;
        }

        throw new IOException(
            "name error: file names are occupied try again in 1 minute and install less than 50 files.");
    }
}
