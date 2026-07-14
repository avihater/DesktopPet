namespace DesktopPet.Controls;

public partial class ChatBubble : System.Windows.Controls.UserControl
{
    private DispatcherTimer? hideTimer;
    private DispatcherTimer? typingTimer;
    private int typingDotIndex = 0;

    public ChatBubble()
    {
        InitializeComponent();
        SetupTypingAnimation();
    }

    /// <summary>
    /// Show a message in the bubble with fade-in animation
    /// </summary>
    public void ShowMessage(string message, string? robotName = null, bool autoHide = true)
    {
        NameLabel.Text = robotName ?? "小Q";
        MessageText.Text = message;

        // Hide typing indicator
        TypingIndicator.Visibility = Visibility.Collapsed;
        typingTimer?.Stop();

        // Animate in
        this.Visibility = Visibility.Visible;
        AnimateBubble(true);

        // Auto-hide after delay
        if (autoHide)
        {
            hideTimer?.Stop();
            hideTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(Math.Max(3, message.Length * 0.08))
            };
            hideTimer.Tick += (s, e) =>
            {
                hideTimer.Stop();
                HideBubble();
            };
            hideTimer.Start();
        }
    }

    /// <summary>
    /// Show streaming text (append chunks as they arrive)
    /// </summary>
    public void AppendStreamChunk(string chunk)
    {
        if (this.Visibility != Visibility.Visible)
        {
            this.Visibility = Visibility.Visible;
            AnimateBubble(true);
            NameLabel.Text = "小Q";
            MessageText.Text = "";
        }

        TypingIndicator.Visibility = Visibility.Collapsed;
        typingTimer?.Stop();

        MessageText.Text += chunk;
    }

    /// <summary>
    /// Show typing indicator
    /// </summary>
    public void ShowTyping()
    {
        this.Visibility = Visibility.Visible;
        AnimateBubble(true);
        NameLabel.Text = "小Q";
        MessageText.Text = "";
        TypingIndicator.Visibility = Visibility.Visible;
        typingTimer?.Start();
    }

    /// <summary>
    /// Finish streaming and optionally auto-hide
    /// </summary>
    public void FinishStreaming(bool autoHide = true)
    {
        TypingIndicator.Visibility = Visibility.Collapsed;
        typingTimer?.Stop();

        if (autoHide && !string.IsNullOrEmpty(MessageText.Text))
        {
            hideTimer?.Stop();
            hideTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(Math.Max(3, MessageText.Text.Length * 0.08))
            };
            hideTimer.Tick += (s, e) =>
            {
                hideTimer.Stop();
                HideBubble();
            };
            hideTimer.Start();
        }
    }

    /// <summary>
    /// Hide the bubble with fade-out animation
    /// </summary>
    public void HideBubble()
    {
        hideTimer?.Stop();
        typingTimer?.Stop();
        AnimateBubble(false);
    }

    private void AnimateBubble(bool show)
    {
        var storyboard = new Storyboard();

        var scaleX = new DoubleAnimation
        {
            To = show ? 1 : 0,
            Duration = TimeSpan.FromMilliseconds(250),
            EasingFunction = show
                ? new ElasticEase { Oscillations = 1, Springiness = 5 }
                : new SineEase { EasingMode = EasingMode.EaseIn }
        };
        Storyboard.SetTarget(scaleX, BubbleScale);
        Storyboard.SetTargetProperty(scaleX, new PropertyPath("ScaleX"));
        storyboard.Children.Add(scaleX);

        var scaleY = new DoubleAnimation
        {
            To = show ? 1 : 0,
            Duration = TimeSpan.FromMilliseconds(250),
            EasingFunction = show
                ? new ElasticEase { Oscillations = 1, Springiness = 5 }
                : new SineEase { EasingMode = EasingMode.EaseIn }
        };
        Storyboard.SetTarget(scaleY, BubbleScale);
        Storyboard.SetTargetProperty(scaleY, new PropertyPath("ScaleY"));
        storyboard.Children.Add(scaleY);

        storyboard.Completed += (s, e) =>
        {
            if (!show)
            {
                this.Visibility = Visibility.Collapsed;
                MessageText.Text = "";
            }
        };

        storyboard.Begin();
    }

    private void SetupTypingAnimation()
    {
        typingTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(350)
        };
        typingTimer.Tick += (s, e) =>
        {
            Dot1.Opacity = typingDotIndex == 0 ? 0.9 : 0.3;
            Dot2.Opacity = typingDotIndex == 1 ? 0.9 : 0.3;
            Dot3.Opacity = typingDotIndex == 2 ? 0.9 : 0.3;
            typingDotIndex = (typingDotIndex + 1) % 3;
        };
    }
}
