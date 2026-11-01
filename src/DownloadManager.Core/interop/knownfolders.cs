using System.Runtime.InteropServices;
namespace DownloadManager.Core.Interop;
    internal static class KnownFolders
    {
        // Downloads folder GUID
        private static readonly Guid DownloadsFolderId =
            new Guid("374DE290-123F-4565-9164-39C4925E467B");

        [DllImport("Shell32.dll")]
            private static extern int SHGetKnownFolderPath(
                ref Guid rfid,
                uint dwFlags,
                IntPtr hToken,
                out IntPtr ppszPath);
            internal static string GetDownloadsFolderPath()
            {
                Guid folderId = DownloadsFolderId;
            
                int result = SHGetKnownFolderPath(
                    ref folderId,
                    0,
                    IntPtr.Zero,
                    out IntPtr pathPointer);
            
                try
                {
                    Marshal.ThrowExceptionForHR(result);
            
                    return Marshal.PtrToStringUni(pathPointer)
                        ?? throw new InvalidOperationException("Folder path error.");
                }
                finally
                {
                    if (pathPointer != IntPtr.Zero)
                    {
                        Marshal.FreeCoTaskMem(pathPointer);
                    }
                }
            }
            // folders list
            internal static string GetVideosFolderPath()
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
            }
            
            internal static string GetMusicFolderPath()
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            }
            
            internal static string GetPicturesFolderPath()
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            }
            
            internal static string GetDocumentsFolderPath()
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
    }
