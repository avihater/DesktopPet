namespace DesktopPet.Controls;

public partial class PetRobot : System.Windows.Controls.UserControl
{
    // Animation timers
    private DispatcherTimer? blinkTimer;
    private DispatcherTimer? wanderTimer;
    private DispatcherTimer? antennaTimer;
    private DispatcherTimer? armSwingTimer;

    // Animation storyboards
    private Storyboard? breatheStoryboard;
    private Storyboard? wanderStoryboard;

    // Robot state
    private bool isInteracting = false;
    private double wanderRange = 30;
    private double baseLeft = 0;
    private double baseTop = 0;

    public PetRobot()
    {
        InitializeComponent();
        this.Loaded += PetRobot_Loaded;
        this.MouseEnter += PetRobot_MouseEnter;
        this.MouseLeave += PetRobot_MouseLeave;
        this.MouseLeftButtonDown += PetRobot_MouseLeftButtonDown;
    }

    private void PetRobot_Loaded(object sender, RoutedEventArgs e)
    {
        baseLeft = Canvas.GetLeft(this);
        baseTop = Canvas.GetTop(this);
    }

    public void StartAnimations()
    {
        StartBreathing();
        StartBlinking();
        StartAntennaSway();
        StartArmSwing();
        StartWandering();
    }

    public void StopAnimations()
    {
        blinkTimer?.Stop();
        wanderTimer?.Stop();
        antennaTimer?.Stop();
        armSwingTimer?.Stop();
        breatheStoryboard?.Stop();
        wanderStoryboard?.Stop();
    }

    #region Breathing Animation

    private void StartBreathing()
    {
        breatheStoryboard = new Storyboard();

        var scaleAnim = new DoubleAnimation
        {
            From = 1.0,
            To = 1.03,
            Duration = TimeSpan.FromSeconds(2.0),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        Storyboard.SetTarget(scaleAnim, BreathScale);
        Storyboard.SetTargetProperty(scaleAnim, new PropertyPath("ScaleX"));
        breatheStoryboard.Children.Add(scaleAnim);

        var scaleAnimY = new DoubleAnimation
        {
            From = 1.0,
            To = 1.03,
            Duration = TimeSpan.FromSeconds(2.0),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        Storyboard.SetTarget(scaleAnimY, BreathScale);
        Storyboard.SetTargetProperty(scaleAnimY, new PropertyPath("ScaleY"));
        breatheStoryboard.Children.Add(scaleAnimY);

        breatheStoryboard.Begin();
    }

    #endregion

    #region Blinking Animation

    private void StartBlinking()
    {
        blinkTimer = new DispatcherTimer();
        blinkTimer.Tick += BlinkTimer_Tick;
        SetRandomBlinkInterval();
        blinkTimer.Start();
    }

    private void SetRandomBlinkInterval()
    {
        if (blinkTimer != null)
        {
            blinkTimer.Interval = TimeSpan.FromMilliseconds(
                new Random().Next(2000, 5000));
        }
    }

    private void BlinkTimer_Tick(object? sender, EventArgs e)
    {
        if (!isInteracting)
            DoBlink();
        SetRandomBlinkInterval();
    }

    private void DoBlink()
    {
        var blinkStoryboard = new Storyboard { Duration = TimeSpan.FromMilliseconds(300) };
        double[] eyes = { 1.0, 0.05, 1.0 };
        double[] times = { 0, 0.5, 1.0 };

        EnsureScaleTransform(LeftEyeWhite);
        EnsureScaleTransform(RightEyeWhite);
        EnsureScaleTransform(LeftPupil);
        EnsureScaleTransform(RightPupil);

        AddBlinkAnimation(blinkStoryboard, LeftEyeWhite, eyes, times);
        AddBlinkAnimation(blinkStoryboard, RightEyeWhite, eyes, times);
        AddBlinkAnimation(blinkStoryboard, LeftPupil, eyes, times);
        AddBlinkAnimation(blinkStoryboard, RightPupil, eyes, times);

        blinkStoryboard.Begin();
    }

    private void AddBlinkAnimation(Storyboard sb, FrameworkElement target,
        double[] values, double[] times)
    {
        var anim = new DoubleAnimationUsingKeyFrames();
        for (int i = 0; i < values.Length; i++)
        {
            anim.KeyFrames.Add(new SplineDoubleKeyFrame(
                values[i],
                KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300 * times[i])),
                new KeySpline(0.3, 0.0, 1.0, 1.0)));
        }
        Storyboard.SetTarget(anim, target);
        Storyboard.SetTargetProperty(anim,
            new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
        sb.Children.Add(anim);
    }

    private void EnsureScaleTransform(FrameworkElement element)
    {
        if (element.RenderTransform is not ScaleTransform)
        {
            element.RenderTransform = new ScaleTransform(1, 1);
            element.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
        }
    }

    #endregion

    #region Antenna Sway

    private void StartAntennaSway()
    {
        antennaTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        antennaTimer.Tick += AntennaTimer_Tick;
        antennaTimer.Start();
    }

    private double antennaAngle = 0;
    private double antennaDirection = 1;

    private void AntennaTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            if (isInteracting) return;
            antennaAngle += 0.15 * antennaDirection;
            if (Math.Abs(antennaAngle) > 4)
                antennaDirection *= -1;
            AntennaSway.Angle = antennaAngle;
        }
        catch { }
    }

    #endregion

    #region Arm Swing

    private void StartArmSwing()
    {
        armSwingTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        armSwingTimer.Tick += ArmSwingTimer_Tick;
        armSwingTimer.Start();
    }

    private double armSwingPhase = 0;

    private void ArmSwingTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            if (isInteracting) return;
            armSwingPhase += 0.05;
            LeftArmSwing.Angle = Math.Sin(armSwingPhase) * 3;
            RightArmSwing.Angle = Math.Sin(armSwingPhase + Math.PI) * 3;
        }
        catch { }
    }

    #endregion

    #region Wandering

    private void StartWandering()
    {
        wanderTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(6)
        };
        wanderTimer.Tick += WanderTimer_Tick;
        wanderTimer.Start();
    }

    private void WanderTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            if (isInteracting) return;
            DoWander();
        }
        catch { }
    }

    private void DoWander()
    {
        var random = new Random();
        double targetX = baseLeft + (random.NextDouble() - 0.5) * wanderRange;
        double targetY = baseTop + (random.NextDouble() - 0.5) * 15;

        wanderStoryboard?.Stop();
        wanderStoryboard = new Storyboard();

        var moveXAnim = new DoubleAnimation
        {
            To = targetX,
            Duration = TimeSpan.FromSeconds(3),
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        Storyboard.SetTarget(moveXAnim, this);
        Storyboard.SetTargetProperty(moveXAnim, new PropertyPath("(Canvas.Left)"));
        wanderStoryboard.Children.Add(moveXAnim);

        var moveYAnim = new DoubleAnimation
        {
            To = targetY,
            Duration = TimeSpan.FromSeconds(3),
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        Storyboard.SetTarget(moveYAnim, this);
        Storyboard.SetTargetProperty(moveYAnim, new PropertyPath("(Canvas.Top)"));
        wanderStoryboard.Children.Add(moveYAnim);

        wanderStoryboard.Begin();
    }

    #endregion

    #region Interaction Visual Feedback

    public void OnMouseDown()
    {
        isInteracting = true;

        var squish = new Storyboard();
        var squishX = new DoubleAnimation
        {
            From = 1.03, To = 0.92,
            Duration = TimeSpan.FromMilliseconds(150),
            AutoReverse = true,
            EasingFunction = new ElasticEase { Oscillations = 2, Springiness = 3 }
        };
        Storyboard.SetTarget(squishX, BreathScale);
        Storyboard.SetTargetProperty(squishX, new PropertyPath("ScaleX"));

        var squishY = new DoubleAnimation
        {
            From = 1.03, To = 1.08,
            Duration = TimeSpan.FromMilliseconds(150),
            AutoReverse = true,
            EasingFunction = new ElasticEase { Oscillations = 2, Springiness = 3 }
        };
        Storyboard.SetTarget(squishY, BreathScale);
        Storyboard.SetTargetProperty(squishY, new PropertyPath("ScaleY"));

        squish.Children.Add(squishX);
        squish.Children.Add(squishY);
        squish.Begin();
    }

    public void OnMouseUp()
    {
        isInteracting = false;

        var bounce = new Storyboard();
        var bounceX = new DoubleAnimation
        {
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new ElasticEase { Oscillations = 2, Springiness = 5 }
        };
        Storyboard.SetTarget(bounceX, BreathScale);
        Storyboard.SetTargetProperty(bounceX, new PropertyPath("ScaleX"));

        var bounceY = new DoubleAnimation
        {
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new ElasticEase { Oscillations = 2, Springiness = 5 }
        };
        Storyboard.SetTarget(bounceY, BreathScale);
        Storyboard.SetTargetProperty(bounceY, new PropertyPath("ScaleY"));

        bounce.Children.Add(bounceX);
        bounce.Children.Add(bounceY);
        bounce.Begin();
    }

    private void PetRobot_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        Head.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x5A, 0xA0, 0xE9));
    }

    private void PetRobot_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        Head.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x4A, 0x90, 0xD9));
    }

    private void PetRobot_MouseLeftButtonDown(object sender,
        System.Windows.Input.MouseButtonEventArgs e)
    {
        e.Handled = false; // Let parent handle drag
    }

    #endregion

    #region Public Methods

    public void SetListeningState(bool listening)
    {
        if (listening)
        {
            StatusDot.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x44, 0x44));
            AntennaBall.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x44, 0x44));
        }
        else
        {
            StatusDot.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00, 0xFF, 0x88));
            AntennaBall.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00, 0xFF, 0x88));
        }
    }

    public void SetSpeakingState(bool speaking)
    {
        if (speaking)
        {
            StatusDot.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0xCC, 0x00));
            AntennaBall.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0xCC, 0x00));
            Mouth.Visibility = Visibility.Collapsed;
        }
        else
        {
            StatusDot.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00, 0xFF, 0x88));
            AntennaBall.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00, 0xFF, 0x88));
            Mouth.Visibility = Visibility.Visible;
        }
    }

    #endregion
}
