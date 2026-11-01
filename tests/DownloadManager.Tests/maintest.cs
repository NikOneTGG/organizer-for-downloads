namespace DownloadManager.Tests;

public class MoveFileTests : IDisposable
{
    private readonly string _testFolder;
    private readonly string _sourceFolder;
    private readonly string _destinationFolder;

    public MoveFileTests()
    {
        _testFolder = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        _sourceFolder = Path.Combine(_testFolder, "Source");
        _destinationFolder = Path.Combine(_testFolder, "Destination");

        Directory.CreateDirectory(_sourceFolder);
        Directory.CreateDirectory(_destinationFolder);
    }

    private string CreateSourceFile(string fileName)
    {
        string path = Path.Combine(_sourceFolder, fileName);
        File.WriteAllText(path, "test");

        return path;
    }

    [Fact]
    public void MoveFile_WhenRenamingDisabled_KeepsOriginalName()
    {
        string sourcePath = CreateSourceFile("original.mp4");

        Core.DownloadManager.MoveFile(
            sourcePath,
            _destinationFolder,
            "Video",
            false,
            (_, _) => throw new InvalidOperationException());

        Assert.False(File.Exists(sourcePath));
        Assert.True(File.Exists(
            Path.Combine(_destinationFolder, "original.mp4")));
    }

    [Fact]
    public void MoveFile_WhenNameOccupied_AddsNumber()
    {
        string sourcePath = CreateSourceFile("original.mp4");

        File.WriteAllText(
            Path.Combine(_destinationFolder, "original.mp4"),
            "existing");

        Core.DownloadManager.MoveFile(
            sourcePath,
            _destinationFolder,
            "Video",
            false,
            (_, _) => throw new InvalidOperationException());

        Assert.True(File.Exists(
            Path.Combine(_destinationFolder, "original.mp4")));

        Assert.True(File.Exists(
            Path.Combine(_destinationFolder, "original (1).mp4")));

        Assert.False(File.Exists(sourcePath));
    }

    [Fact]
    public void MoveFile_WhenRenamingEnabled_UsesNewName()
    {
        string sourcePath = CreateSourceFile("original.mp4");

        Core.DownloadManager.MoveFile(
            sourcePath,
            _destinationFolder,
            "Video",
            true,
            (_, _) => "renamed.mp4");

        Assert.False(File.Exists(sourcePath));

        Assert.True(File.Exists(
            Path.Combine(_destinationFolder, "renamed.mp4")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testFolder))
            Directory.Delete(_testFolder, true);
    }
}
