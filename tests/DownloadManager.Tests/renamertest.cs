using DownloadManager.Core;

namespace DownloadManager.Tests;

public class Erenametests : IDisposable
{
    private readonly string _testFolder;

    private readonly DateTime _testDate =
        new(2026, 11, 2, 14, 30, 0);
    
    private static readonly RenameSettings TestSettings = new(
    "конскаязалупа",
    true,
    true);

    public Erenametests()
    {
        _testFolder = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(_testFolder);
    }

    [Fact]
    public void GetFileName_ReturnsCorrectFormat()
    {
        string result = Erename.GetFileName(
            "original.mp4",
            _testFolder,
            _testDate,
            TestSettings);
    
        Assert.Matches(
            @"^конскаязалупа_02\.11\.2026_1430_([1-9]|[1-4][0-9]|50)\.mp4$",
            result);
    }

    [Fact]
    public void GetFileName_PreservesExtension()
    {
        string result = Erename.GetFileName(
            "document.pdf",
            _testFolder,
            _testDate,
            TestSettings);

        Assert.EndsWith(".pdf", result);
    }

    [Fact]
    public void GetFileName_ReturnsFreeNumber()
    {
        for (int i = 1; i <= 50; i++)
        {
            if (i == 17)
                continue;

            string fileName =
                $"конскаязалупа_02.11.2026_1430_{i}.mp4";

            File.WriteAllText(
                Path.Combine(_testFolder, fileName),
                "");
        }

        string result = Erename.GetFileName(
            "original.mp4",
            _testFolder,
            _testDate,
            TestSettings);

        Assert.Equal(
            "конскаязалупа_02.11.2026_1430_17.mp4",
            result);
    }

    [Fact]
    public void GetFileName_ThrowsWhenAllNamesAreOccupied()
    {
        for (int i = 1; i <= 50; i++)
        {
            string fileName =
                $"конскаязалупа_02.11.2026_1430_{i}.mp4";

            File.WriteAllText(
                Path.Combine(_testFolder, fileName),
                "");
        }

        Assert.Throws<IOException>(() =>
            Erename.GetFileName(
                "original.mp4",
                _testFolder,
                _testDate,
                TestSettings));
    }

    [Fact]
    public void GetFileName_WhenRenamingDisabled_ReturnsOriginalName()
    {
        RenameSettings settings = new("", false, false);
    
        string result = Erename.GetFileName(
            "original.mp4",
            _testFolder,
            _testDate,
            settings);
    
        Assert.Equal("original.mp4", result);
    }

    [Fact]
    public void GetFileName_UsesOriginalNameWhenPrefixIsEmpty()
    {
        RenameSettings settings = new("", true, false);

        string result = Erename.GetFileName(
            "video.mp4",
            _testFolder,
            _testDate,
            settings);

        Assert.Equal("video_02.11.2026_1430.mp4", result);
    }

    [Fact]
    public void GetFileName_UsesPrefixInsteadOfOriginalName()
    {
        RenameSettings settings = new("MyName", false, false);

        string result = Erename.GetFileName(
            "video.mp4",
            _testFolder,
            _testDate,
            settings);

        Assert.Equal("MyName.mp4", result);
    }

    [Fact]
    public void GetFileName_AddsRandomNumberToOriginalName()
    {
        RenameSettings settings = new("", false, true);

        string result = Erename.GetFileName(
            "video.mp4",
            _testFolder,
            _testDate,
            settings);

        Assert.Matches(
            @"^video_([1-9]|[1-4][0-9]|50)\.mp4$",
            result);
    }


    public void Dispose()
    {
        Directory.Delete(_testFolder, true);
    }
}