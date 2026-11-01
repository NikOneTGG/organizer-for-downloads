using System.Collections.Concurrent;
using System.Threading.Channels;
namespace DownloadManager.Core
{
    class DownloadManager
    {
        private static readonly Channel<string> FileQueue = Channel.CreateUnbounded<string>();
        private static readonly ConcurrentDictionary<string, byte> SeenFiles = new(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, byte> IgnoredPaths = new(StringComparer.OrdinalIgnoreCase);
        static async Task Main(string[] args)
        {
            // folder to watch
            using var check = new FileSystemWatcher(
                Interop.KnownFolders.GetDownloadsFolderPath());

            check.NotifyFilter = NotifyFilters.FileName
                | NotifyFilters.DirectoryName;

            // event
            check.Created += OnCreated;
            check.Renamed += OnRenamed;
            check.Error += OnError;

            check.Filter = "*.*";
            check.IncludeSubdirectories = false;

            Task processingTask = ProcessQueueAsync();

            check.EnableRaisingEvents = true;

            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();

            check.EnableRaisingEvents = false;

            FileQueue.Writer.TryComplete();

            await processingTask;
        }

        // event action
        private static void OnCreated(object sender, FileSystemEventArgs e)
        {
            if (Ext.FileExt.IsTemporaryFile(e.FullPath))
                return;

            if (!SeenFiles.TryAdd(e.FullPath, 0))
                return;

            _ = CheckCreatedFileAsync(e.FullPath);
        }

        // delay 4 file download finish
        private static async Task CheckCreatedFileAsync(string path)
        {
            await Task.Delay(3000);

            if (File.Exists(path + ".part"))
            {
                SeenFiles.TryRemove(path, out _);
                return;
            }

            if (!FileQueue.Writer.TryWrite(path))
            {
                SeenFiles.TryRemove(path, out _);
            }
        }

        private static void OnRenamed(object sender, RenamedEventArgs e)
        {
            if (IgnoredPaths.TryRemove(e.FullPath, out _))
                return;

            QueueFile(e.FullPath);
        }

        private static void OnError(object sender, ErrorEventArgs e)
        {
            Console.WriteLine($"Watcher error: {e.GetException().Message}");
        }

        private static bool IsFileReady(string path)
        {
            try
            {
                using var stream = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.None);
        
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

        private static void QueueFile(string path)
        {
            if (Ext.FileExt.IsTemporaryFile(path))
                return;

            if (!SeenFiles.TryAdd(path, 0))
                return;

            if (!FileQueue.Writer.TryWrite(path))
            {
                SeenFiles.TryRemove(path, out _);
            }
        }

        private static async Task<bool> WaitForFileReady(string path)
        {
            const int maxAttempts = 10;
            const int delayMs = 500;
            const int requiredStableChecks = 2;

            long previousSize = -1;
            int stableChecks = 0;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                try
                {
                    if (File.Exists(path + ".part"))
                    {
                        stableChecks = 0;
                        await Task.Delay(delayMs);
                        continue;
                    }
                    var fileInfo = new FileInfo(path);
                    fileInfo.Refresh();

                    if (!fileInfo.Exists)
                        return false;

                    long currentSize = fileInfo.Length;

                    if (currentSize == previousSize)
                    {
                        stableChecks++;
                    }
                    else
                    {
                        stableChecks = 0;
                    }

                    previousSize = currentSize;

                    if (stableChecks >= requiredStableChecks && IsFileReady(path))
                        return true;
                }
                catch (IOException)
                {
                    stableChecks = 0;
                }
                catch (UnauthorizedAccessException)
                {
                    stableChecks = 0;
                }

                await Task.Delay(delayMs);
            }

            return false;
        }

        // async queue
        private static async Task ProcessQueueAsync()
        {
            await foreach (string path in FileQueue.Reader.ReadAllAsync())
            {
                try
                {
                    if (!File.Exists(path))
                        continue;

                    bool isReady = await WaitForFileReady(path);

                    if (!isReady)
                    {
                        Console.WriteLine($"File not ready: {path}");
                        continue;
                    }
                    if (File.Exists(path + ".part"))
                        continue;

                    string extension = Path.GetExtension(path);
                    string category = Ext.FileExt.GetCategory(extension);
                    string? destinationFolder = GetDestinationFolder(category);

                    destinationFolder ??= Interop.KnownFolders.GetDownloadsFolderPath();

                    MoveFile(path, destinationFolder, category);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Processing error: {path} - {ex.Message}");
                }
                finally
                {
                    SeenFiles.TryRemove(path, out _);
                }
            }
        }
        // folder to move file
        private static string? GetDestinationFolder(string category)
        {
            return category switch
            {
                "Video" => Interop.KnownFolders.GetVideosFolderPath(),
                "Music" => Interop.KnownFolders.GetMusicFolderPath(),
                "Image" => Interop.KnownFolders.GetPicturesFolderPath(),
                "Document" => Interop.KnownFolders.GetDocumentsFolderPath(),
                _ => null
            };
        }

        // move method
        private static void MoveFile(
            string sourcePath,
            string destinationFolder,
            string category)
        {
            MoveFile(
                sourcePath,
                destinationFolder,
                category,
                Erename.IsEnabled,
                Erename.GetFileName);
        }
        
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

            if (Path.GetFullPath(sourcePath).Equals(
                Path.GetFullPath(destinationPath),
                StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!renameFile)
            {
                int number = 1;

                while (File.Exists(destinationPath))
                {
                    string name = Path.GetFileNameWithoutExtension(fileName);
                    string extension = Path.GetExtension(fileName);

                    destinationPath = Path.Combine(
                        destinationFolder,
                        $"{name} ({number}){extension}");

                    number++;
                }
            }

            bool isDownloadsFolder = category == "Unknown" && renameFile;

            if (isDownloadsFolder)
                IgnoredPaths.TryAdd(destinationPath, 0);

            try
            {
                File.Move(sourcePath, destinationPath);
                Console.WriteLine($"Moved: {sourcePath} -> {destinationPath}");
            }
            catch
            {
                if (isDownloadsFolder)
                    IgnoredPaths.TryRemove(destinationPath, out _);

                throw;
            }
        }
    }
}