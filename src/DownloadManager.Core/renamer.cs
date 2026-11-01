
namespace DownloadManager.Core;

internal sealed record RenameSettings(
    string Prefix,
    bool AddDateTime,
    bool AddRandomNumber);

internal static class Erename
{
    // rename settings
    private static readonly RenameSettings Settings = new(
    /* file rename */ "",
    /* add date and time */ true,
    /* add random number 1-50 */ false);

    internal static bool IsEnabled =>
        !string.IsNullOrWhiteSpace(Settings.Prefix)
        || Settings.AddDateTime
        || Settings.AddRandomNumber;

    internal static string GetFileName(
        string sourcePath,
        string destinationFolder)
    {
        return GetFileName(
            sourcePath,
            destinationFolder,
            DateTime.Now,
            Settings);
    }

    internal static string GetFileName(
        string sourcePath,
        string destinationFolder,
        DateTime dateTime)
    {
        return GetFileName(
            sourcePath,
            destinationFolder,
            dateTime,
            Settings);
    }

    internal static string GetFileName(
        string sourcePath,
        string destinationFolder,
        DateTime dateTime,
        RenameSettings settings)
    {
        string extension = Path.GetExtension(sourcePath);
        string originalName = Path.GetFileNameWithoutExtension(sourcePath);

        List<string> parts = new();

        if (!string.IsNullOrWhiteSpace(settings.Prefix))
            parts.Add(settings.Prefix);

        if (settings.AddDateTime)
            parts.Add(dateTime.ToString("dd.MM.yyyy_HHmm"));

        int[] numbers = settings.AddRandomNumber
            ? Enumerable.Range(1, 50).ToArray()
            : new[] { 0 };

        // shuffle numbers
        for (int i = numbers.Length - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (numbers[i], numbers[j]) = (numbers[j], numbers[i]);
        }

        foreach (int number in numbers)
        {
            List<string> nameParts = new(parts);

            if (settings.AddRandomNumber)
                nameParts.Add(number.ToString());

            string fileName = nameParts.Count > 0
                ? string.Join("_", nameParts) + extension
                : originalName + extension;

            string destinationPath = Path.Combine(destinationFolder, fileName);

            if (!File.Exists(destinationPath)
                && !Directory.Exists(destinationPath))
            {
                return fileName;
            }
        }

        throw new IOException(
            "name error: file names are occupied try again in 1 minute and install less than 50 files.");
    }
}
