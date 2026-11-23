
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

    [Fact]
    public void LoadOrCreate_CreatesDefaultConfigWithAutostartOn()
    {
        AppSettings settings =
            AppSettings.LoadOrCreate(_configPath);

        Assert.Equal("on", settings.Autostart);
        Assert.True(settings.AutostartEnabled);
        Assert.Contains("\"Autostart\": \"on\"", File.ReadAllText(_configPath));
    }

    [Theory]
    [InlineData("off", false)]
    [InlineData("OFF", false)]
    [InlineData(" off ", false)]
    [InlineData("on", true)]
    [InlineData("On", true)]
    [InlineData("", true)]
    [InlineData("anything", true)]
    public void LoadOrCreate_ReadsAutostartValue(string value, bool expected)
    {
        File.WriteAllText(
            _configPath,
            $$"""
            {
                "Namefile": "",
                "Dateandtime": true,
                "Randomnum": true,
                "Autostart": "{{value}}"
            }
            """);

        AppSettings settings =
            AppSettings.LoadOrCreate(_configPath);

        Assert.Equal(expected, settings.AutostartEnabled);
    }

    [Fact]
    public void LoadOrCreate_AutostartIsOnWhenKeyIsMissing()
    {
        File.WriteAllText(
            _configPath,
            """
            {
                "Namefile": "MyName_",
                "Dateandtime": false,
                "Randomnum": false
            }
            """);

        AppSettings settings =
            AppSettings.LoadOrCreate(_configPath);

        Assert.True(settings.AutostartEnabled);
        Assert.Equal("MyName_", settings.Namefile);
    }

    [Fact]
    public void LoadOrCreate_AutostartIsOnWhenValueIsNull()
    {
        File.WriteAllText(
            _configPath,
            """
            {
                "Autostart": null
            }
            """);

        AppSettings settings =
            AppSettings.LoadOrCreate(_configPath);

        Assert.True(settings.AutostartEnabled);
    }

    [Fact]
    public void LoadOrCreate_AutostartIsOnWhenJsonIsInvalid()
    {
        File.WriteAllText(_configPath, "{ invalid json");

        AppSettings settings =
            AppSettings.LoadOrCreate(_configPath);

        Assert.True(settings.AutostartEnabled);
    }

    [Fact]
    public void Save_WritesSettingsThatLoadOrCreateReadsBack()
    {
        AppSettings expected = new()
        {
            Namefile = "Photo",
            Dateandtime = false,
            Randomnum = true,
            Autostart = "off"
        };

        AppSettings.Save(_configPath, expected);

        AppSettings actual = AppSettings.LoadOrCreate(_configPath);

        Assert.Equal(expected, actual);
        Assert.False(actual.AutostartEnabled);
    }

    [Fact]
    public void Save_OverwritesExistingConfigAndLeavesNoTempFile()
    {
        AppSettings.LoadOrCreate(_configPath);

        AppSettings.Save(_configPath, new AppSettings { Namefile = "New" });

        Assert.Equal("New", AppSettings.LoadOrCreate(_configPath).Namefile);
        Assert.False(File.Exists(_configPath + ".tmp"));
    }

    [Fact]
    public void Save_KeepsNonAsciiNameReadableInFile()
    {
        AppSettings.Save(_configPath, new AppSettings { Namefile = "Фото ёж" });

        Assert.Contains("Фото ёж", File.ReadAllText(_configPath));
        Assert.Equal("Фото ёж", AppSettings.LoadOrCreate(_configPath).Namefile);
    }

    [Fact]
    public void Save_WritesAutostartAsOnOffString()
    {
        AppSettings.Save(_configPath, new AppSettings { Autostart = "off" });

        Assert.Contains("\"Autostart\": \"off\"", File.ReadAllText(_configPath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testFolder))
            Directory.Delete(_testFolder, true);
    }
}
