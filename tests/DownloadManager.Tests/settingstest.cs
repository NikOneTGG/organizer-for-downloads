
using DownloadManager.Core;

namespace DownloadManager.Tests;

public class AppSettingsTests : IDisposable
{
    private readonly string _testFolder;
    private readonly string _configPath;

    public AppSettingsTests()
    {
        _testFolder = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(_testFolder);

        _configPath = Path.Combine(
            _testFolder,
            "config.json");
    }

    [Fact]
    public void LoadOrCreate_CreatesDefaultConfig()
    {
        AppSettings settings =
            AppSettings.LoadOrCreate(_configPath);

        Assert.True(File.Exists(_configPath));
        Assert.Equal("", settings.Namefile);
        Assert.True(settings.Dateandtime);
        Assert.True(settings.Randomnum);
    }

    [Fact]
    public void LoadOrCreate_ReadsExistingConfig()
    {
        File.WriteAllText(
            _configPath,
            """
            {
                "Namefile": "MyName_",
                "Dateandtime": false,
                "Randomnum": true
            }
            """);

        AppSettings settings =
            AppSettings.LoadOrCreate(_configPath);

        Assert.Equal("MyName_", settings.Namefile);
        Assert.False(settings.Dateandtime);
        Assert.True(settings.Randomnum);
    }

    [Fact]
    public void LoadOrCreate_UsesDefaultsWhenJsonIsInvalid()
    {
        File.WriteAllText(_configPath, "{ invalid json");

        AppSettings settings =
            AppSettings.LoadOrCreate(_configPath);

        Assert.Equal("", settings.Namefile);
        Assert.True(settings.Dateandtime);
        Assert.True(settings.Randomnum);

        Assert.Equal("{ invalid json", File.ReadAllText(_configPath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testFolder))
            Directory.Delete(_testFolder, true);
    }
}
