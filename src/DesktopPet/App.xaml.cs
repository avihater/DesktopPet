using System.Drawing;
using System.Runtime.InteropServices;
using DesktopPet.Services;

namespace DesktopPet;

public partial class App : System.Windows.Application
{
    private WinForms.NotifyIcon? trayIcon;
    private MainWindow? mainWindow;

    // Global hotkey
    private const int HOTKEY_ID = 9001;
    private const int MOD_CONTROL = 0x0002;
    private const int MOD_SHIFT = 0x0004;
    private const int VK_Q = 0x51;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    // Service instances
    public ConfigService ConfigService { get; }
    public AiService AiService { get; }
    public VoiceService? VoiceService { get; private set; }
    public ControlService ControlService { get; }

    public App()
    {
        ConfigService = new ConfigService();
        AiService = new AiService(ConfigService);
        ControlService = new ControlService(ConfigService);

        // Global exception handlers
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize voice service (non-critical)
        try
        {
            VoiceService = new VoiceService(ConfigService);
        }
        catch { }

        mainWindow = new MainWindow();
        mainWindow.Show();

        SetupTrayIcon();
        RegisterGlobalHotkey();
    }

    private void RegisterGlobalHotkey()
    {
        // Register Ctrl+Shift+Q as voice trigger
        var helper = new System.Windows.Interop.WindowInteropHelper(mainWindow!);
        var hwnd = helper.EnsureHandle();

        // Need to hook into window messages for hotkey
        var source = System.Windows.Interop.HwndSource.FromHwnd(hwnd);
        source?.AddHook(HwndHook);

        RegisterHotKey(hwnd, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, VK_Q);
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_HOTKEY = 0x0312;

        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            mainWindow?.TriggerVoiceInput();
            handled = true;
        }

        return IntPtr.Zero;
    }

    #region Exception Handlers

    private void App_DispatcherUnhandledException(object sender,
        System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"UI Exception: {e.Exception.Message}");
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender,
        UnhandledExceptionEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"Unhandled Exception: {e.ExceptionObject}");
    }

    private void TaskScheduler_UnobservedTaskException(object? sender,
        UnobservedTaskExceptionEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"Task Exception: {e.Exception.Message}");
        e.SetObserved();
    }

    #endregion

    #region Tray Icon

    private void SetupTrayIcon()
    {
        trayIcon = new WinForms.NotifyIcon
        {
            Text = "桌面宠物助手 - 小Q\n快捷键 Ctrl+Shift+Q 唤醒",
            Visible = true
        };

        // Create robot face icon programmatically
        using var bitmap = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        using var bodyBrush = new SolidBrush(System.Drawing.Color.FromArgb(0x4A, 0x90, 0xD9));
        using var eyeBrush = new SolidBrush(System.Drawing.Color.White);
        using var pupilBrush = new SolidBrush(System.Drawing.Color.FromArgb(0x1A, 0x1A, 0x2E));
        using var antennaPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(0x88, 0x99, 0xAA), 2);

        g.FillRectangle(bodyBrush, 4, 6, 24, 18);
        g.DrawLine(antennaPen, 16, 6, 16, 0);
        g.FillEllipse(new SolidBrush(System.Drawing.Color.FromArgb(0x00, 0xFF, 0x88)), 13, 0, 6, 6);
        g.FillEllipse(eyeBrush, 9, 10, 8, 9);
        g.FillEllipse(eyeBrush, 17, 10, 8, 9);
        g.FillEllipse(pupilBrush, 12, 13, 4, 5);
        g.FillEllipse(pupilBrush, 20, 13, 4, 5);
        g.DrawArc(new System.Drawing.Pen(System.Drawing.Color.FromArgb(0x2C, 0x5F, 0x8A), 1.5f), 10, 14, 12, 8, 0, -180);

        var iconHandle = bitmap.GetHicon();
        trayIcon.Icon = Icon.FromHandle(iconHandle);

        // Context menu
        var contextMenu = new WinForms.ContextMenuStrip();

        var showHideItem = new WinForms.ToolStripMenuItem("显示/隐藏宠物");
        showHideItem.Click += (s, ev) =>
        {
            if (mainWindow != null)
            {
                if (mainWindow.Visibility == Visibility.Visible)
                    mainWindow.Hide();
                else
                    mainWindow.Show();
            }
        };
        contextMenu.Items.Add(showHideItem);

        var chatItem = new WinForms.ToolStripMenuItem("打开聊天");
        chatItem.Click += (s, ev) => mainWindow?.OpenChatWindow();
        contextMenu.Items.Add(chatItem);

        var voiceItem = new WinForms.ToolStripMenuItem("语音输入 (Ctrl+Shift+Q)");
        voiceItem.Click += (s, ev) => mainWindow?.TriggerVoiceInput();
        contextMenu.Items.Add(voiceItem);

        contextMenu.Items.Add(new WinForms.ToolStripSeparator());

        var settingsItem = new WinForms.ToolStripMenuItem("设置");
        settingsItem.Click += (s, ev) => mainWindow?.OpenSettings();
        contextMenu.Items.Add(settingsItem);

        contextMenu.Items.Add(new WinForms.ToolStripSeparator());

        var exitItem = new WinForms.ToolStripMenuItem("退出");
        exitItem.Click += (s, ev) =>
        {
            trayIcon?.Dispose();
            UnregisterHotKey(
                new System.Windows.Interop.WindowInteropHelper(mainWindow!).Handle,
                HOTKEY_ID);
            VoiceService?.Dispose();
            Shutdown();
        };
        contextMenu.Items.Add(exitItem);

        trayIcon.ContextMenuStrip = contextMenu;

        trayIcon.DoubleClick += (s, ev) =>
        {
            if (mainWindow != null)
            {
                if (mainWindow.Visibility == Visibility.Visible)
                    mainWindow.Hide();
                else
                    mainWindow.Show();
            }
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        VoiceService?.Dispose();
        trayIcon?.Dispose();
        base.OnExit(e);
    }

    #endregion
}
