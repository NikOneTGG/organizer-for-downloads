using System.Text.Encodings.Web;
using System.Text.Json;

namespace DownloadManager.Core;

internal sealed record AppSettings
{
    /* name change for every installed file */public string Namefile { get; init; } = "";
    /* add date and time in file name like: filename_dateandtime */public bool Dateandtime { get; init; } = true;
    /* add random number 1-50 in name like: filename_randmomnum */public bool Randomnum { get; init; } = true;
    public string Autostart { get; init; } = "on";

    internal bool AutostartEnabled =>
        !string.Equals(Autostart.Trim(), "off", StringComparison.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    internal static void Save(string configPath, AppSettings settings)
    {
        string json = JsonSerializer.Serialize(settings, JsonOptions);
        string tempPath = configPath + ".tmp";

        File.WriteAllText(tempPath, json);
        File.Move(tempPath, configPath, true);
    }

    internal static AppSettings LoadOrCreate(string configPath)
    {
        if (!File.Exists(configPath))
        {
            AppSettings defaults = new();

            const string defaultConfig = """
            {
              /* change text in "" to set one name for every installed file */"Namefile": "",
              /* change it from false to true to add date and time into file name */"Dateandtime": true,
              /* change it from false to true to add random number 1-50 into file name */ "Randomnum": true,
              /* change it from "on" to "off" to stop the app from starting with Windows */ "Autostart": "on"
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
                Namefile = settings.Namefile ?? "",
                Autostart = settings.Autostart ?? "on"
            };
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
    }
}
