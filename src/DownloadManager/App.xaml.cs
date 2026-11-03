using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Diagnostics;
using Microsoft.Win32;
using Application = System.Windows.Application;

namespace DownloadManager;

public partial class App : Application
{
    private const uint WM_APP = 0x8000;
    private const uint WM_TRAY = WM_APP + 1;

    private const uint WM_RBUTTONUP = 0x0205;
    private const uint WM_CONTEXTMENU = 0x007B;

    private const uint NIM_ADD = 0x00000000;
    private const uint NIM_DELETE = 0x00000002;

    private const uint NIF_MESSAGE = 0x00000001;
    private const uint NIF_ICON = 0x00000002;
    private const uint NIF_TIP = 0x00000004;

    private const uint MF_STRING = 0x00000000;

    private const uint TPM_RIGHTBUTTON = 0x0002;
    private const uint TPM_RETURNCMD = 0x0100;

    private const uint ID_PAUSE = 1;
    private const uint ID_EXIT = 2;
    private const uint ID_CONFIGURE = 3;

    private const int HWND_MESSAGE = -3;

    private const uint WM_NULL = 0x0000;

    private const int ERROR_SUCCESS = 0;

    private const string WindowClassName = "OrganizeMyDownloadsTrayWindow";
    private const string runRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string startupValueName = "OrganizeMyDownloads";

    private HwndSource? _messageSource;
    private IntPtr _iconHandle;
    private bool _isPaused;
    private bool _trayIconAdded;

    private bool _isExiting;

    private uint _taskbarCreatedMessage;

    private CancellationTokenSource? _coreCts;
    private Task? _coreTask;

private IntPtr LoadTrayIcon()
{
    var resourceInfo = Application.GetResourceStream(
        new Uri("pack://application:,,,/assets/icon.ico"));

    if (resourceInfo == null)
        return IntPtr.Zero;

    using Stream stream = resourceInfo.Stream;
    using var memoryStream = new MemoryStream();

    stream.CopyTo(memoryStream);

    using var icon = new System.Drawing.Icon(
        new MemoryStream(memoryStream.ToArray()));

    return icon.Handle == IntPtr.Zero
        ? IntPtr.Zero
        : CopyIcon(icon.Handle);
}

[DllImport("user32.dll")]
private static extern IntPtr CopyIcon(IntPtr hIcon);

protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        configureAutoStart();

        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        EnsureConfigExists();

        _iconHandle = LoadTrayIcon();

        if (_iconHandle == IntPtr.Zero)
        {
            System.Windows.MessageBox.Show(
                "Failed to load embedded tray icon.",
                "OrganizeMyDownloads",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown();
            return;
        }

        CreateMessageWindow();


        _taskbarCreatedMessage = RegisterWindowMessage("TaskbarCreated");

        AddTrayIcon();


        _coreCts = new CancellationTokenSource();
        _coreTask = DownloadManager.Core.DownloadManager.RunAsync(_coreCts.Token);


    }

    private void configureAutoStart()
    {
        try
        {
            string? executablePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(executablePath))
                return;

            string command = $"\"{executablePath}\"";

            using RegistryKey? runKey = Registry.CurrentUser.CreateSubKey(runRegistryPath);
            if (runKey == null)
                return;

            string? currentCommand = runKey.GetValue(startupValueName) as string;
            if (!string.Equals(currentCommand, command, StringComparison.OrdinalIgnoreCase))
                runKey.SetValue(startupValueName, command, RegistryValueKind.String);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to configure auto-start: {ex}");
        }
    }

    private void CreateMessageWindow()
    {
        var parameters = new HwndSourceParameters(WindowClassName)
        {
            Width = 0,
            Height = 0,
            ParentWindow = new IntPtr(HWND_MESSAGE),
            WindowStyle = 0
        };

        _messageSource = new HwndSource(parameters);
        _messageSource.AddHook(WindowProcedure);
    }

    private void AddTrayIcon()
    {
        if (_messageSource == null || _iconHandle == IntPtr.Zero)
            return;

        var data = new NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _messageSource.Handle,
            uID = 1,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
            uCallbackMessage = WM_TRAY,
            hIcon = _iconHandle,
            szTip = "OrganizeMyDownloads"
        };

        _trayIconAdded = Shell_NotifyIcon(NIM_ADD, ref data);

        if (!_trayIconAdded)
        {
            System.Windows.MessageBox.Show(
                "Failed to add the tray icon.",
                "OrganizeMyDownloads",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown();
        }
    }

    private IntPtr WindowProcedure(
        IntPtr hwnd,
        int message,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled)
    {
        if (_taskbarCreatedMessage != 0 &&
            message == _taskbarCreatedMessage)
        {
            AddTrayIcon();
            handled = true;
            return IntPtr.Zero;
        }

        if ((uint)message == WM_TRAY)
        {
            uint notification = unchecked((uint)lParam.ToInt64());

            if (notification == WM_RBUTTONUP ||
                notification == WM_CONTEXTMENU)
            {
                ShowTrayMenu();
            }

            handled = true;
        }

        return IntPtr.Zero;
    }

    private string GetConfigPath()
    {
        return Path.Combine(
            AppContext.BaseDirectory,
            "config.json");
    }

    private void EnsureConfigExists()
    {
        string configPath = GetConfigPath();


        if (File.Exists(configPath))
            return;

        const string defaultConfig = """
        {
          /* change text in "" to set one name for every installed file */"Namefile": "",
          /* change it from false to true to add date and time into file name */"Dateandtime": true,
          /* change it from false to true to add random number 1-50 into file name */ "Randomnum": true
        }
        """;

        File.WriteAllText(configPath, defaultConfig);
    }

    private void ShowTrayMenu()
    {
        IntPtr menu = CreatePopupMenu();

        if (menu == IntPtr.Zero)
            return;

        string pauseText = _isPaused ? "Unpause" : "Pause";

        AppendMenu(menu, MF_STRING, ID_CONFIGURE, "Configure");
        AppendMenu(menu, MF_STRING, ID_PAUSE, pauseText);
        AppendMenu(menu, MF_STRING, ID_EXIT, "Exit");

        GetCursorPos(out POINT cursor);

        SetForegroundWindow(_messageSource!.Handle);

        uint selectedItem = TrackPopupMenu(
            menu,
            TPM_RIGHTBUTTON | TPM_RETURNCMD,
            cursor.X,
            cursor.Y,
            0,
            _messageSource.Handle,
            IntPtr.Zero);

        DestroyMenu(menu);

        PostMessage(
            _messageSource.Handle,
            WM_NULL,
            IntPtr.Zero,
            IntPtr.Zero);

        switch (selectedItem)
        {
            case ID_CONFIGURE:
                OpenConfig();
                break;

            case ID_PAUSE:
                TogglePause();
                break;

            case ID_EXIT:
                _ = ExitAsync();
                break;
        }
    }

    private void OpenConfig()
    {
        string configPath = GetConfigPath();


        try
        {
            EnsureConfigExists();

            Process.Start(new ProcessStartInfo
            {
                FileName = "notepad.exe",
                ArgumentList = { configPath },
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Failed to open configuration:\n{ex.Message}",
                "OrganizeMyDownloads",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void TogglePause()
    {
        _isPaused = !_isPaused;
        DownloadManager.Core.DownloadManager.SetPaused(_isPaused);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        RemoveTrayIcon();

        if (_messageSource != null)
        {
            _messageSource.RemoveHook(WindowProcedure);
            _messageSource.Dispose();
            _messageSource = null;
        }

        if (_iconHandle != IntPtr.Zero)
        {
            DestroyIcon(_iconHandle);
            _iconHandle = IntPtr.Zero;
        }

        base.OnExit(e);
    }

    private async Task ExitAsync()
    {
        if (_isExiting)
            return;
    
        _isExiting = true;
    
        try
        {
            _coreCts?.Cancel();
    
            if (_coreTask != null)
                await _coreTask;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Shutdown error: {ex}");
        }
        finally
        {
            _coreCts?.Dispose();
            _coreCts = null;
    
            Shutdown();
        }
    }

    private void RemoveTrayIcon()
    {
        if (!_trayIconAdded || _messageSource == null)
            return;

        var data = new NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _messageSource.Handle,
            uID = 1
        };

        Shell_NotifyIcon(NIM_DELETE, ref data);
        _trayIconAdded = false;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;

        public uint dwState;
        public uint dwStateMask;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;

        public uint uTimeoutOrVersion;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;

        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Shell_NotifyIcon(
        uint dwMessage,
        ref NOTIFYICONDATA lpData);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern uint RegisterWindowMessage(string lpString);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool AppendMenu(
        IntPtr hMenu,
        uint uFlags,
        uint uIDNewItem,
        string lpNewItem);

    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint TrackPopupMenu(
        IntPtr hMenu,
        uint uFlags,
        int x,
        int y,
        int nReserved,
        IntPtr hWnd,
        IntPtr prcRect);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(
        IntPtr hWnd,
        uint msg,
        IntPtr wParam,
        IntPtr lParam);
}