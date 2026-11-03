using System.Text.Json;

namespace DownloadManager.Core;

internal sealed record AppSettings
{
    /* name change for every installed file */public string Namefile { get; init; } = "";
    /* add date and time in file name like: filename_dateandtime */public bool Dateandtime { get; init; } = true;
    /* add random number 1-50 in name like: filename_randmomnum */public bool Randomnum { get; init; } = true;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    internal static AppSettings LoadOrCreate(string configPath)
    {
        if (!File.Exists(configPath))
        {
            AppSettings defaults = new();

            const string defaultConfig = """
            {
              /* change text in "" to set one name for every installed file */"Namefile": "",
              /* change it from false to true to add date and time into file name */"Dateandtime": true,
              /* change it from false to true to add random number 1-50 into file name */ "Randomnum": true
            }
            """;

            File.WriteAllText(configPath, defaultConfig);
            return defaults;
        }

        try
        {
            string json = File.ReadAllText(configPath);

            AppSettings settings =
                JsonSerializer.Deserialize<AppSettings>(
                    json,
                    JsonOptions) ?? new();

            return settings with
            {
                Namefile = settings.Namefile ?? ""
            };
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
    }
}
