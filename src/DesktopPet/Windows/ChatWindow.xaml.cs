using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using DesktopPet.Models;
using DesktopPet.Services;

namespace DesktopPet.Windows;

public partial class ChatWindow : Window
{
    private readonly AiService _aiService;
    private readonly ConfigService _configService;
    private readonly ControlService _controlService;
    private bool _isProcessing = false;

    public ChatWindow(AiService aiService, ConfigService configService, ControlService controlService)
    {
        InitializeComponent();
        _aiService = aiService;
        _configService = configService;
        _controlService = controlService;

        this.Loaded += ChatWindow_Loaded;
        this.Closing += ChatWindow_Closing;
    }

    private void ChatWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Set placeholder text
        InputTextBox.Text = (string)InputTextBox.Tag;
        InputTextBox.Foreground = System.Windows.Media.Brushes.Gray;

        // Restore history
        foreach (var msg in _aiService.History)
        {
            AddMessageToView(msg);
        }
        ScrollToBottom();
    }

    private void ChatWindow_Closing(object? sender,
        System.ComponentModel.CancelEventArgs e)
    {
        // Hide instead of close
        e.Cancel = true;
        this.Hide();
    }

    #region Input Handling

    private void InputTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (InputTextBox.Text == (string)InputTextBox.Tag)
        {
            InputTextBox.Text = "";
            InputTextBox.Foreground = System.Windows.Media.Brushes.Black;
        }
    }

    private void InputTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(InputTextBox.Text))
        {
            InputTextBox.Text = (string)InputTextBox.Tag;
            InputTextBox.Foreground = System.Windows.Media.Brushes.Gray;
        }
    }

    private async void InputTextBox_KeyDown(object sender,
        System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter &&
            !System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift) &&
            !System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightShift))
        {
            e.Handled = true;
            await SendMessage();
        }
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        await SendMessage();
    }

    #endregion

    #region Send Message

    private async Task SendMessage()
    {
        var text = InputTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(text) || text == (string)InputTextBox.Tag || _isProcessing)
            return;

        // Clear input
        InputTextBox.Text = "";
        _isProcessing = true;
        SendButton.IsEnabled = false;

        // Add user message to view
        var userMsg = new ChatMessage { Role = "user", Content = text };
        AddMessageToView(userMsg);

        // Check API key
        if (string.IsNullOrWhiteSpace(_configService.Config.ApiKey))
        {
            var noKeyMsg = new ChatMessage
            {
                Role = "assistant",
                Content = "主人还没有设置 API Key 呢~ 请先在设置中配置 DeepSeek 的 API Key，小Q才能和你聊天哦！(◕‿◕)"
            };
            AddMessageToView(noKeyMsg);
            _isProcessing = false;
            SendButton.IsEnabled = true;
            return;
        }

        // Create streaming message placeholder
        var streamBubble = CreateStreamingBubble();
        MessagesList.Items.Add(streamBubble);
        ScrollToBottom();

        // Stream response
        var fullResponse = "";
        try
        {
            await foreach (var chunk in _aiService.ChatStreamAsync(text))
            {
                fullResponse += chunk;
                // Update the streaming bubble text
                UpdateStreamingBubble(streamBubble, fullResponse);
                ScrollToBottom();
            }
        }
        catch (Exception ex)
        {
            fullResponse = $"呜~ 出错了：{ex.Message} (´;ω;`)";
            UpdateStreamingBubble(streamBubble, fullResponse);
        }

        // Replace streaming bubble with final styled bubble
        MessagesList.Items.Remove(streamBubble);
        var assistantMsg = new ChatMessage { Role = "assistant", Content = fullResponse };
        AddMessageToView(assistantMsg);

        // Check for commands
        var cmdResult = ParseAndExecuteCommand(fullResponse);
        if (cmdResult != null)
        {
            var cmdMsg = new ChatMessage { Role = "assistant", Content = cmdResult };
            AddMessageToView(cmdMsg);
        }

        _isProcessing = false;
        SendButton.IsEnabled = true;
        ScrollToBottom();
    }

    #endregion

    #region Command Parsing

    private string? ParseAndExecuteCommand(string response)
    {
        try
        {
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

    #region Message Display

    private void AddMessageToView(ChatMessage msg)
    {
        var container = new StackPanel { Margin = new Thickness(0, 2, 0, 2) };

        if (msg.Role == "user")
        {
            // User message (right-aligned, blue)
            var border = new Border
            {
                Style = (Style)FindResource("UserBubbleStyle")
            };
            var textBlock = new TextBlock
            {
                Text = msg.Content,
                FontSize = 13,
                Foreground = System.Windows.Media.Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20
            };
            border.Child = textBlock;

            // Time label
            var timeLabel = new TextBlock
            {
                Text = msg.Timestamp.ToString("HH:mm"),
                FontSize = 10,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x99, 0xAA, 0xBB)),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Margin = new Thickness(0, 1, 14, 0)
            };

            container.Children.Add(border);
            container.Children.Add(timeLabel);
        }
        else
        {
            // Assistant message (left-aligned, white)
            var border = new Border
            {
                Style = (Style)FindResource("AssistantBubbleStyle")
            };
            var textBlock = new TextBlock
            {
                Text = msg.Content,
                FontSize = 13,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1A, 0x1A, 0x2E)),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20
            };
            border.Child = textBlock;

            // Name label
            var nameLabel = new TextBlock
            {
                Text = _configService.Config.RobotName,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x4A, 0x90, 0xD9)),
                Margin = new Thickness(16, 0, 0, 1)
            };

            container.Children.Add(nameLabel);
            container.Children.Add(border);
        }

        MessagesList.Items.Add(container);
    }

    private Border CreateStreamingBubble()
    {
        var border = new Border
        {
            Style = (Style)FindResource("AssistantBubbleStyle"),
            Tag = "streaming"
        };

        var textBlock = new TextBlock
        {
            Text = "正在思考...",
            FontSize = 13,
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1A, 0x1A, 0x2E)),
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 20,
            FontStyle = FontStyles.Italic
        };
        border.Child = textBlock;

        var container = new StackPanel();
        var nameLabel = new TextBlock
        {
            Text = _configService.Config.RobotName + " 正在输入...",
            FontSize = 11,
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x4A, 0x90, 0xD9)),
            Margin = new Thickness(16, 0, 0, 1)
        };
        container.Children.Add(nameLabel);
        container.Children.Add(border);
        container.Tag = "streaming";

        return border;
    }

    private void UpdateStreamingBubble(Border bubble, string text)
    {
        if (bubble.Child is TextBlock tb)
        {
            tb.Text = text;
            tb.FontStyle = FontStyles.Normal;
        }
    }

    private void ScrollToBottom()
    {
        ChatScrollViewer.ScrollToEnd();
    }

    #endregion
}
