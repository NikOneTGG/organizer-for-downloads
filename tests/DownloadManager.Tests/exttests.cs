using DownloadManager.Core.Ext;

namespace DownloadManager.Tests;

public class Exttest
{
    [Theory]
    [InlineData(".png", "Image")]
    [InlineData(".jpg", "Image")]
    [InlineData(".mp4", "Video")]
    [InlineData(".mkv", "Video")]
    [InlineData(".mp3", "Music")]
    [InlineData(".flac", "Music")]
    [InlineData(".pdf", "Document")]
    [InlineData(".docx", "Document")]
    public void GetCategory_ReturnsCorrectCategory(
        string extension,
        string expected)
    {
        string result = FileExt.GetCategory(extension);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetCategory_IsCaseInsensitive()
    {
        string result = FileExt.GetCategory(".MP4");

        Assert.Equal("Video", result);
    }

    [Fact]
    public void GetCategory_ReturnsUnknownForUnsupportedExtension()
    {
        string result = FileExt.GetCategory(".zip");

        Assert.Equal("Unknown", result);
    }

    [Fact]
    public async Task WaitForFileReady_WaitsUntilLongDownloadFinishes()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.mp4");
    
        try
        {
            await File.WriteAllTextAsync(path, "first part");
    
            Task<bool> readinessTask = Core.DownloadManager.WaitForFileReady(
                path,
                maxAttempts: 40,
                delayMs: 100,
                requiredStableChecks: 5);
    
            await Task.Delay(250);
    
            await File.AppendAllTextAsync(path, " second part");
            await Task.Delay(250);
    
            await File.AppendAllTextAsync(path, " final part");
    
            bool result = await readinessTask;
    
            Assert.True(result);
            Assert.Equal(
                "first part second part final part",
                await File.ReadAllTextAsync(path));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Theory]
    [InlineData("file.tmp")]
    [InlineData("file.crdownload")]
    [InlineData("file.part")]
    [InlineData("file.download")]
    public void IsTemporaryFile_ReturnsTrue(string path)
    {
        bool result = FileExt.IsTemporaryFile(path);

        Assert.True(result);
    }

    [Theory]
    [InlineData("file.mp4")]
    [InlineData("file.pdf")]
    [InlineData("file.zip")]
    public void IsTemporaryFile_ReturnsFalse(string path)
    {
        bool result = FileExt.IsTemporaryFile(path);

        Assert.False(result);
    }
    [Theory]
    [InlineData("file.PART")]
    [InlineData("file.CrDownload")]
    [InlineData(@"C:\Users\Test\Downloads\file.part")]
    public void IsTemporaryFile_IsCaseInsensitiveAndSupportsFullPaths(string path)
    {
        Assert.True(FileExt.IsTemporaryFile(path));
    }
}