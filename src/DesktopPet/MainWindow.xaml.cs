using System.Runtime.InteropServices;
using DesktopPet.Services;
using DesktopPet.Windows;

namespace DesktopPet;

public partial class MainWindow : Window
{
    // P/Invoke for desktop click-through
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    // Services
    private readonly AiService _aiService;
    private readonly ConfigService _configService;
    private readonly VoiceService? _voiceService;
    private readonly ControlService _controlService;
    private ChatWindow? _chatWindow;

    private static readonly string ConfigPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DesktopPet", "config.json");

    public MainWindow()
    {
        var app = (App)System.Windows.Application.Current;
        _configService = app.ConfigService;
        _aiService = app.AiService;
        _voiceService = app.VoiceService;
        _controlService = app.ControlService;

        InitializeComponent();
        this.Loaded += MainWindow_Loaded;
        this.MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;
        this.MouseLeftButtonUp += MainWindow_MouseLeftButtonUp;
        this.MouseMove += MainWindow_MouseMove;

        // Right-click robot to open chat window
        petRobot.MouseRightButtonDown += PetRobot_RightClick;

        // Subscribe to voice events
        if (_voiceService != null)
        {
            _voiceService.SpeechRecognized += VoiceService_SpeechRecognized;
            _voiceService.SpeechNotRecognized += VoiceService_SpeechNotRecognized;
            _voiceService.StateChanged += VoiceService_StateChanged;
        }
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        MakeWindowClickThrough();
        RestorePosition();
        petRobot.StartAnimations();
    }

    #region Voice Integration

    /// <summary>
    /// Trigger voice input (called by hotkey or tray menu)
    /// </summary>
    public void TriggerVoiceInput()
    {
        if (_voiceService == null || !_voiceService.IsAvailable)
        {
            ShowBubbleMessage("语音服务不可用呢~\n请检查 Windows 语音设置中是否安装了中文语音 (◕‿◕)");
            return;
        }

        // If already listening, cancel
        if (_voiceService.CurrentState == VoiceState.Listening)
        {
            _voiceService.CancelListening();
            return;
        }

        // If currently speaking, stop and start new listening
        if (_voiceService.CurrentState == VoiceState.Speaking)
        {
            _voiceService.StopSpeaking();
            // Small delay to let TTS clean up
            Task.Delay(200).ContinueWith(_ =>
            {
                Dispatcher.Invoke(() =>
                {
                    _voiceService.StartListening();
                    petRobot.SetListeningState(true);
                });
            });
            return;
        }

        // Start listening
        _voiceService.StartListening();
        petRobot.SetListeningState(true);
    }

    /// <summary>
    /// Called when speech was recognized successfully
    /// </summary>
    private async void VoiceService_SpeechRecognized(string text)
    {
        await ProcessVoiceInput(text);
    }

    /// <summary>
    /// Called when recognizer heard sound but couldn't understand it
    /// </summary>
    private void VoiceService_SpeechNotRecognized()
    {
        Dispatcher.Invoke(() =>
        {
            petRobot.SetListeningState(false);
            ShowBubbleMessage("嗯…没听清呢，主人再说一遍？(．ω．)");
        });
    }

    /// <summary>
    /// Core voice processing: send recognized text to AI, display response, speak via TTS
    /// </summary>
    private async Task ProcessVoiceInput(string recognizedText)
    {
        await Dispatcher.InvokeAsync(async () =>
        {
            petRobot.SetListeningState(false);

            // Show what was heard
            ShowBubbleMessage($"🎤 听到了: \"{recognizedText}\"", autoHide: false);

            // Check API key
            if (string.IsNullOrWhiteSpace(_configService.Config.ApiKey))
            {
                ShowBubbleMessage("主人还没设置 API Key 呢~\n请先在设置中配置 DeepSeek 的 API Key！");
                return;
            }

            // Show typing indicator
            chatBubble.ShowTyping();

            // Stream AI response
            var fullResponse = "";
            try
            {
                await foreach (var chunk in _aiService.ChatStreamAsync(recognizedText))
                {
                    fullResponse += chunk;
                    chatBubble.AppendStreamChunk(chunk);
                }
            }
            catch (Exception ex)
            {
                fullResponse = $"呜~ 出错了：{ex.Message}";
                chatBubble.ShowMessage(fullResponse);
            }

            chatBubble.FinishStreaming(autoHide: true);

            // Execute any computer commands embedded in response
            var commandResult = ParseAndExecuteCommand(fullResponse);
            if (commandResult != null)
            {
                await Task.Delay(400);
                ShowBubbleMessage(commandResult);
            }

            // ---- CRITICAL: Speak the response via TTS ----
            if (_voiceService != null && _voiceService.IsAvailable && !string.IsNullOrWhiteSpace(fullResponse))
            {
                _voiceService.Speak(fullResponse);
                petRobot.SetSpeakingState(true);
            }
        });
    }

    private void VoiceService_StateChanged(VoiceState state)
    {
        Dispatcher.Invoke(() =>
        {
            switch (state)
            {
                case VoiceState.Idle:
                    petRobot.SetListeningState(false);
                    petRobot.SetSpeakingState(false);
                    break;
                case VoiceState.Listening:
                    petRobot.SetListeningState(true);
                    petRobot.SetSpeakingState(false);
                    break;
                case VoiceState.Speaking:
                    petRobot.SetListeningState(false);
                    petRobot.SetSpeakingState(true);
                    break;
                case VoiceState.Processing:
                    petRobot.SetListeningState(false);
                    break;
            }
        });
    }

    #endregion

    #region Command Parsing

    /// <summary>
    /// Parse AI response for 【命令:xxx】 markers and execute them
    /// </summary>
    private string? ParseAndExecuteCommand(string response)
    {
        try
        {
            // Look for 【命令:xxx】 pattern
            var match = System.Text.RegularExpressions.Regex.Match(
                response, @"【命令[:：](.*?)】");

            if (match.Success)
            {
                var command = match.Groups[1].Value.Trim();
                var result = _controlService.ExecuteCommand(command);
                return result.IsCommand ? result.Message : null;
            }
        }
        catch { }

        return null;
    }

    #endregion

    #region Window Management

    private void PetRobot_RightClick(object sender,
        System.Windows.Input.MouseButtonEventArgs e)
    {
        OpenChatWindow();
        e.Handled = true;
    }

    public void OpenChatWindow()
    {
        if (_chatWindow == null)
        {
            _chatWindow = new ChatWindow(_aiService, _configService, _controlService);
            _chatWindow.Closed += (s, e) => _chatWindow = null;
        }

        if (_chatWindow.Visibility != Visibility.Visible)
        {
            _chatWindow.Show();
            _chatWindow.Activate();
        }
        else
        {
            _chatWindow.Activate();
        }
    }

    public void ShowBubbleMessage(string message, bool autoHide = true)
    {
        chatBubble.ShowMessage(message, _configService.Config.RobotName, autoHide);
    }

    public void OpenSettings()
    {
        var settingsWindow = new SettingsWindow(_configService);
        settingsWindow.Owner = this;
        settingsWindow.ShowDialog();
    }

    private void MakeWindowClickThrough()
    {
        var helper = new WindowInteropHelper(this);
        var hwnd = helper.EnsureHandle();

        int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        // NOTE: Do NOT add WS_EX_TRANSPARENT — it makes the entire window
        // (including the robot) transparent to mouse clicks.
        // Instead, we rely on WPF's layered window hit testing:
        // Background="{x:Null}" areas pass clicks through naturally.
        exStyle |= WS_EX_LAYERED;       // Required for transparency
        exStyle |= WS_EX_TOOLWINDOW;    // Hide from Alt+Tab
        exStyle |= WS_EX_NOACTIVATE;    // Don't steal focus
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
    }

    #endregion

    #region Drag to Move

    private bool isDragging = false;
    private System.Windows.Point dragStartPoint;

    private void MainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement element && IsPartOfRobot(element))
        {
            isDragging = true;
            dragStartPoint = e.GetPosition(this);
            this.CaptureMouse();
            petRobot.OnMouseDown();
        }
    }

    private void MainWindow_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (isDragging)
        {
            System.Windows.Point currentPoint = e.GetPosition(this);
            Vector delta = currentPoint - dragStartPoint;
            this.Left += delta.X;
            this.Top += delta.Y;
        }
    }

    private void MainWindow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (isDragging)
        {
            isDragging = false;
            this.ReleaseMouseCapture();
            petRobot.OnMouseUp();
            SavePosition();
        }
    }

    private bool IsPartOfRobot(FrameworkElement element)
    {
        DependencyObject? current = element;
        while (current != null)
        {
            if (current is Controls.PetRobot)
                return true;
            current = VisualTreeHelper.GetParent(current);
        }
        return false;
    }

    #endregion

    #region Position Persistence

    private void SavePosition()
    {
        try
        {
            var config = new { Left = this.Left, Top = this.Top };
            string? dir = System.IO.Path.GetDirectoryName(ConfigPath);
            if (dir != null)
            {
                System.IO.Directory.CreateDirectory(dir);
                System.IO.File.WriteAllText(ConfigPath,
                    System.Text.Json.JsonSerializer.Serialize(config));
            }
        }
        catch { }
    }

    private void RestorePosition()
    {
        try
        {
            if (System.IO.File.Exists(ConfigPath))
            {
                var json = System.IO.File.ReadAllText(ConfigPath);
                var config = System.Text.Json.JsonSerializer.Deserialize<PositionConfig>(json);
                if (config != null)
                {
                    this.Left = config.Left;
                    this.Top = config.Top;

                    var screen = WinFormsScreen.PrimaryScreen;
                    if (screen != null)
                    {
                        if (this.Left < screen.WorkingArea.Left)
                            this.Left = screen.WorkingArea.Left;
                        if (this.Top < screen.WorkingArea.Top)
                            this.Top = screen.WorkingArea.Top;
                        if (this.Left + this.Width > screen.WorkingArea.Right)
                            this.Left = screen.WorkingArea.Right - this.Width;
                        if (this.Top + this.Height > screen.WorkingArea.Bottom)
                            this.Top = screen.WorkingArea.Bottom - this.Height;
                    }
                    return;
                }
            }
        }
        catch { }

        var primaryScreen = WinFormsScreen.PrimaryScreen;
        if (primaryScreen != null)
        {
            this.Left = primaryScreen.WorkingArea.Right - this.Width - 20;
            this.Top = primaryScreen.WorkingArea.Bottom - this.Height - 20;
        }
    }

    private class PositionConfig
    {
        public double Left { get; set; }
        public double Top { get; set; }
    }

    #endregion
}
