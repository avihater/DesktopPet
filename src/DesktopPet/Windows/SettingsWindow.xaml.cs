using DesktopPet.Services;

namespace DesktopPet.Windows;

public partial class SettingsWindow : Window
{
    private readonly ConfigService _configService;

    public SettingsWindow(ConfigService configService)
    {
        InitializeComponent();
        _configService = configService;

        this.Loaded += SettingsWindow_Loaded;

        // Temperature slider value display
        TemperatureSlider.ValueChanged += (s, e) =>
        {
            TemperatureLabel.Text = TemperatureSlider.Value.ToString("F1");
        };
    }

    private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var cfg = _configService.Config;

        ApiKeyBox.Password = cfg.ApiKey;
        ApiBaseUrlBox.Text = cfg.ApiBaseUrl;
        ModelNameBox.Text = cfg.ModelName;
        TemperatureSlider.Value = cfg.Temperature;
        TemperatureLabel.Text = cfg.Temperature.ToString("F1");
        RobotNameBox.Text = cfg.RobotName;
        UserNameBox.Text = cfg.UserName;
        UserProfileBox.Text = cfg.UserProfile;
        SystemPromptBox.Text = cfg.SystemPrompt;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var cfg = _configService.Config;

        // Only update API key if something was entered
        if (!string.IsNullOrWhiteSpace(ApiKeyBox.Password))
        {
            cfg.ApiKey = ApiKeyBox.Password;
        }

        cfg.ApiBaseUrl = ApiBaseUrlBox.Text.Trim();
        cfg.ModelName = ModelNameBox.Text.Trim();
        cfg.Temperature = Math.Round(TemperatureSlider.Value, 1);
        cfg.RobotName = RobotNameBox.Text.Trim();
        cfg.UserName = UserNameBox.Text.Trim();
        cfg.UserProfile = UserProfileBox.Text.Trim();
        cfg.SystemPrompt = SystemPromptBox.Text.Trim();

        _configService.Save();

        System.Windows.MessageBox.Show(
            "设置已保存！(◕‿◕)✿",
            "小Q",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        this.Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
