using DownloadManager.Core.Ext;

namespace DownloadManager.Tests;

public class FileExtensionsTests
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
}