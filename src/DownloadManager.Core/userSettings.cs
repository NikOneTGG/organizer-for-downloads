namespace DownloadManager.Core;

public sealed record UserSettings(
    string FileName,
    bool AddDateAndTime,
    bool AddRandomNumber,
    bool AutostartEnabled)
{
    private static readonly string ConfigPath =
        Path.Combine(AppContext.BaseDirectory, "config.json");

    private static readonly object SettingsLock = new();

    public static UserSettings Load()
    {
        AppSettings settings = AppSettings.LoadOrCreate(ConfigPath);

        return new UserSettings(
            settings.Namefile,
            settings.Dateandtime,
            settings.Randomnum,
            settings.AutostartEnabled);
    }

    public static void Update(Func<UserSettings, UserSettings> change)
    {
        lock (SettingsLock)
        {
            UserSettings updated = change(Load());

            AppSettings.Save(ConfigPath, new AppSettings
            {
                Namefile = updated.FileName,
                Dateandtime = updated.AddDateAndTime,
                Randomnum = updated.AddRandomNumber,
                Autostart = updated.AutostartEnabled ? "on" : "off"
            });
        }
    }
}
