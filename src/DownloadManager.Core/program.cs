using System.Collections.Concurrent;
using System.Threading.Channels;

namespace DownloadManager.Core;

public class DownloadManager
{
    private const int QueueCapacity = 512;
    private const int PendingWorkerCount = 8;

    private static readonly Channel<string> PendingQueue =
        Channel.CreateBounded<string>(new BoundedChannelOptions(QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        });

    private static readonly Channel<string> FileQueue =
        Channel.CreateBounded<string>(new BoundedChannelOptions(QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });

    private static readonly ConcurrentDictionary<string, byte> SeenFiles =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly ConcurrentDictionary<string, byte> IgnoredPaths =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly object PauseLock = new();
    private static TaskCompletionSource _resumeSignal = CreateCompletedSignal();

    private static TaskCompletionSource CreateCompletedSignal()
    {
        var signal = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        signal.SetResult();
        return signal;
    }

    public static void SetPaused(bool paused)
    {
        lock (PauseLock)
        {
            if (paused)
            {
                if (_resumeSignal.Task.IsCompleted)
                    _resumeSignal = new TaskCompletionSource(
                        TaskCreationOptions.RunContinuationsAsynchronously);
            }
            else
            {
                _resumeSignal.TrySetResult();
            }
        }
    }

    private static Task WaitUntilResumedAsync(CancellationToken token)
    {
        Task resumeTask;
        lock (PauseLock)
            resumeTask = _resumeSignal.Task;

        return resumeTask.WaitAsync(token);
    }

    public static async Task RunAsync(CancellationToken cancellationToken)
    {
        string downloadsPath = Interop.KnownFolders.GetDownloadsFolderPath();

        using var watcher = new FileSystemWatcher(downloadsPath)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName,
            Filter = "*.*",
            IncludeSubdirectories = false
        };

        watcher.Created += OnCreated;
        watcher.Renamed += OnRenamed;

        Task processingTask = ProcessQueueAsync(cancellationToken);
        Task[] pendingWorkers = Enumerable.Range(0, PendingWorkerCount)
            .Select(_ => ProcessPendingQueueAsync(cancellationToken))
            .ToArray();

        watcher.EnableRaisingEvents = true;

        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
        finally
        {
            watcher.EnableRaisingEvents = false;
            PendingQueue.Writer.TryComplete();

            try
            {
                await Task.WhenAll(pendingWorkers);
            }
            finally
            {
                FileQueue.Writer.TryComplete();
                await processingTask;
            }
        }
    }

    private static void OnCreated(object sender, FileSystemEventArgs e) =>
        ScheduleCheck(e.FullPath);

    private static void OnRenamed(object sender, RenamedEventArgs e)
    {
        if (IgnoredPaths.TryRemove(e.FullPath, out _))
            return;

        ScheduleCheck(e.FullPath);
    }

    private static void ScheduleCheck(string path)
    {
        if (Ext.FileExt.IsTemporaryFile(path) || !SeenFiles.TryAdd(path, 0))
            return;

        try
        {
            PendingQueue.Writer.WriteAsync(path).AsTask().GetAwaiter().GetResult();
        }
        catch (ChannelClosedException)
        {
            SeenFiles.TryRemove(path, out _);
        }
    }

    private static async Task ProcessPendingQueueAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (string path in PendingQueue.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    await Task.Delay(3000, cancellationToken);

                    if (File.Exists(path + ".part"))
                    {
                        SeenFiles.TryRemove(path, out _);
                        continue;
                    }

                    await FileQueue.Writer.WriteAsync(path, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    SeenFiles.TryRemove(path, out _);
                    break;
                }
                catch (ChannelClosedException)
                {
                    SeenFiles.TryRemove(path, out _);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }

    private static bool IsFileReady(string path)
    {
        try
        {
            using var stream = new FileStream(
                path, FileMode.Open, FileAccess.Read, FileShare.None);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    internal static async Task<bool> WaitForFileReady(
        string path,
        int maxAttempts = 120,
        int delayMs = 500,
        int requiredStableChecks = 10,
        CancellationToken cancellationToken = default)
    {
        long previousSize = -1;
        int stableChecks = 0;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (!File.Exists(path))
                    return false;

                if (File.Exists(path + ".part") ||
                    File.Exists(path + ".crdownload"))
                {
                    stableChecks = 0;
                    previousSize = -1;
                }
                else
                {
                    var fileInfo = new FileInfo(path);
                    fileInfo.Refresh();

                    if (!fileInfo.Exists)
                        return false;

                    long currentSize = fileInfo.Length;
                    stableChecks = currentSize == previousSize
                        ? stableChecks + 1
                        : 0;
                    previousSize = currentSize;

                    if (stableChecks >= requiredStableChecks && IsFileReady(path))
                        return true;
                }
            }
            catch (IOException)
            {
                stableChecks = 0;
                previousSize = -1;
            }
            catch (UnauthorizedAccessException)
            {
                stableChecks = 0;
                previousSize = -1;
            }

            await Task.Delay(delayMs, cancellationToken);
        }

        return false;
    }

    private static async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (string path in FileQueue.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    await WaitUntilResumedAsync(cancellationToken);

                    if (!File.Exists(path) ||
                        !await WaitForFileReady(path, cancellationToken: cancellationToken) ||
                        File.Exists(path + ".part"))
                        continue;

                    string category = Ext.FileExt.GetCategory(Path.GetExtension(path));
                    string destination = GetDestinationFolder(category)
                        ?? Interop.KnownFolders.GetDownloadsFolderPath();

                    await WaitUntilResumedAsync(cancellationToken);
                    MoveFile(path, destination, category);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception)
                {
                    // A single inaccessible or invalid file must not stop monitoring.
                }
                finally
                {
                    SeenFiles.TryRemove(path, out _);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }

    private static string? GetDestinationFolder(string category) => category switch
    {
        "Video" => Interop.KnownFolders.GetVideosFolderPath(),
        "Music" => Interop.KnownFolders.GetMusicFolderPath(),
        "Image" => Interop.KnownFolders.GetPicturesFolderPath(),
        "Document" => Interop.KnownFolders.GetDocumentsFolderPath(),
        _ => null
    };

    private static void MoveFile(string sourcePath, string destinationFolder, string category) =>
        MoveFile(sourcePath, destinationFolder, category,
            Erename.IsEnabled, Erename.GetFileName);

    internal static void MoveFile(
        string sourcePath,
        string destinationFolder,
        string category,
        bool renameFile,
        Func<string, string, string> getFileName)
    {
        Directory.CreateDirectory(destinationFolder);

        string fileName = renameFile
            ? getFileName(sourcePath, destinationFolder)
            : Path.GetFileName(sourcePath);

        string destinationPath = Path.Combine(destinationFolder, fileName);

        if (string.Equals(
            Path.GetFullPath(sourcePath),
            Path.GetFullPath(destinationPath),
            StringComparison.OrdinalIgnoreCase))
            return;

        if (!renameFile)
        {
            string name = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);
            int number = 1;

            while (File.Exists(destinationPath))
                destinationPath = Path.Combine(
                    destinationFolder, $"{name} ({number++}){extension}");
        }

        bool trackRename = category == "Unknown" && renameFile;
        if (trackRename)
            IgnoredPaths.TryAdd(destinationPath, 0);

        try
        {
            File.Move(sourcePath, destinationPath);
        }
        catch
        {
            if (trackRename)
                IgnoredPaths.TryRemove(destinationPath, out _);
            throw;
        }
    }
}
