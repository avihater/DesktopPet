using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DesktopPet.Services;

/// <summary>
/// Computer control service - execute system operations
/// </summary>
public class ControlService
{
    private readonly ConfigService _config;

    // P/Invoke for system control
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool LockWorkStation();

    private const uint KEYEVENTF_KEYDOWN = 0x0000;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const byte VK_VOLUME_UP = 0xAF;
    private const byte VK_VOLUME_DOWN = 0xAE;
    private const byte VK_VOLUME_MUTE = 0xAD;
    private const byte VK_LWIN = 0x5B;

    public ControlService(ConfigService config)
    {
        _config = config;
    }

    /// <summary>
    /// Try to parse and execute a computer control command
    /// Returns true if a command was recognized and executed
    /// </summary>
    public CommandResult ExecuteCommand(string input)
    {
        var text = input.ToLower().Trim();

        // Volume control
        if (text.Contains("音量") || text.Contains("声音"))
        {
            if (text.Contains("大") || text.Contains("高") || text.Contains("加") || text.Contains("增加"))
            {
                VolumeUp();
                return new CommandResult(true, "音量已调大 🔊");
            }
            if (text.Contains("小") || text.Contains("低") || text.Contains("减") || text.Contains("降低"))
            {
                VolumeDown();
                return new CommandResult(true, "音量已调小 🔉");
            }
            if (text.Contains("静音") || text.Contains("关"))
            {
                Mute();
                return new CommandResult(true, "已静音 🔇");
            }
        }

        // Open applications
        if (text.Contains("打开") || text.Contains("启动") || text.Contains("运行"))
        {
            var app = TryOpenApplication(text);
            if (app != null)
                return new CommandResult(true, $"已打开 {app} ✅");
        }

        // Search web
        if (text.Contains("搜索") || text.Contains("搜一下") || text.Contains("查一下") ||
            text.Contains("帮我搜") || text.Contains("百度"))
        {
            var query = ExtractSearchQuery(text);
            if (!string.IsNullOrWhiteSpace(query))
            {
                SearchWeb(query);
                return new CommandResult(true, $"正在搜索: {query} 🔍");
            }
        }

        // Lock screen
        if (text.Contains("锁屏") || text.Contains("锁定") || text.Contains("锁住"))
        {
            LockScreen();
            return new CommandResult(true, "屏幕已锁定 🔒");
        }

        // Screenshot
        if (text.Contains("截图") || text.Contains("截屏"))
        {
            TakeScreenshot();
            return new CommandResult(true, "截图工具已打开 📸");
        }

        // Clipboard operations
        if (text.Contains("复制"))
        {
            var content = ExtractAfterKeyword(text, "复制");
            if (!string.IsNullOrWhiteSpace(content))
            {
                SetClipboard(content.Trim());
                return new CommandResult(true, $"已复制到剪贴板 📋");
            }
        }

        if (text.Contains("粘贴"))
        {
            Paste();
            return new CommandResult(true, "粘贴完成 📋");
        }

        // Calculator
        if (text.Contains("计算器"))
        {
            OpenProcess("calc.exe", "计算器");
            return new CommandResult(true, "计算器已打开 🔢");
        }

        // Notepad
        if (text.Contains("记事本") || text.Contains("便签"))
        {
            OpenProcess("notepad.exe", "记事本");
            return new CommandResult(true, "记事本已打开 📝");
        }

        // No command recognized
        return new CommandResult(false, null);
    }

    #region Volume Control

    private void VolumeUp()
    {
        for (int i = 0; i < 5; i++)
        {
            keybd_event(VK_VOLUME_UP, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            keybd_event(VK_VOLUME_UP, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
    }

    private void VolumeDown()
    {
        for (int i = 0; i < 5; i++)
        {
            keybd_event(VK_VOLUME_DOWN, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            keybd_event(VK_VOLUME_DOWN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
    }

    private void Mute()
    {
        keybd_event(VK_VOLUME_MUTE, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
        keybd_event(VK_VOLUME_MUTE, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    #endregion

    #region Application Launch

    private string? TryOpenApplication(string text)
    {
        // Common app mappings
        var appMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "浏览器", "https://www.baidu.com" },
            { "edge", "microsoft-edge:" },
            { "chrome", "chrome" },
            { "微信", "wechat" },
            { "qq", "qq" },
            { "钉钉", "dingtalk" },
            { "记事本", "notepad" },
            { "计算器", "calc" },
            { "画图", "mspaint" },
            { "资源管理器", "explorer" },
            { "文件管理器", "explorer" },
            { "任务管理器", "taskmgr" },
            { "cmd", "cmd" },
            { "控制面板", "control" },
            { "设置", "ms-settings:" },
            { "vscode", "code" },
            { "visual studio", "devenv" },
            { "终端", "wt" },
            { "powershell", "powershell" },
        };

        foreach (var (name, path) in appMap)
        {
            if (text.Contains(name))
            {
                OpenProcess(path, name);
                return name;
            }
        }

        return null;
    }

    private void OpenProcess(string target, string displayName)
    {
        try
        {
            if (target.StartsWith("http"))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = target,
                    UseShellExecute = true
                });
            }
            else if (target.Contains(":"))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = target,
                    UseShellExecute = true
                });
            }
            else
            {
                Process.Start(target);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Failed to open {displayName}: {ex.Message}");
        }
    }

    #endregion

    #region Web Search

    private void SearchWeb(string query)
    {
        var encoded = Uri.EscapeDataString(query);
        var url = $"https://www.baidu.com/s?wd={encoded}";
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }

    private string ExtractSearchQuery(string text)
    {
        // Remove common prefixes
        var prefixes = new[] { "搜索", "搜一下", "查一下", "帮我搜", "百度" };
        foreach (var prefix in prefixes)
        {
            int idx = text.IndexOf(prefix);
            if (idx >= 0)
            {
                return text[(idx + prefix.Length)..].Trim();
            }
        }
        return text.Trim();
    }

    #endregion

    #region System Operations

    private void LockScreen()
    {
        LockWorkStation();
    }

    private void TakeScreenshot()
    {
        // Simulate Win+Shift+S (Windows screenshot tool)
        keybd_event(VK_LWIN, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
        keybd_event(0x10, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero); // Shift
        keybd_event(0x53, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero); // S
        keybd_event(0x53, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(0x10, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_LWIN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    private void SetClipboard(string text)
    {
        Thread staThread = new Thread(() =>
        {
            try
            {
                System.Windows.Clipboard.SetText(text);
            }
            catch { }
        });
        staThread.SetApartmentState(ApartmentState.STA);
        staThread.Start();
        staThread.Join(2000);
    }

    private void Paste()
    {
        // Simulate Ctrl+V
        keybd_event(0x11, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero); // Ctrl
        keybd_event(0x56, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero); // V
        keybd_event(0x56, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(0x11, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    #endregion

    #region Helpers

    private string ExtractAfterKeyword(string text, string keyword)
    {
        int idx = text.IndexOf(keyword);
        if (idx >= 0)
            return text[(idx + keyword.Length)..].Trim();
        return "";
    }

    #endregion
}

/// <summary>
/// Result of a command execution attempt
/// </summary>
public class CommandResult
{
    public bool IsCommand { get; }
    public string? Message { get; }

    public CommandResult(bool isCommand, string? message)
    {
        IsCommand = isCommand;
        Message = message;
    }
}
