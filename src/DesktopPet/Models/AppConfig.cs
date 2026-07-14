using System.Text.Json.Serialization;

namespace DesktopPet.Models;

/// <summary>
/// Application configuration (stored as JSON)
/// </summary>
public class AppConfig
{
    // AI Settings
    public string AiProvider { get; set; } = "deepseek";
    public string ApiKey { get; set; } = "";
    public string ApiBaseUrl { get; set; } = "https://api.deepseek.com";
    public string ModelName { get; set; } = "deepseek-chat";
    public double Temperature { get; set; } = 0.7;
    public int MaxHistoryRounds { get; set; } = 20;

    // Persona
    public string RobotName { get; set; } = "小Q";
    public string SystemPrompt { get; set; } = "你是一个可爱的Q版桌面机器人助手，名字叫小Q。" +
        "你的性格活泼、贴心、有点小幽默。你称呼用户为'主人'。\n" +
        "以下是关于主人的信息：{user_profile}\n" +
        "请用口语化、带一点颜文字的方式回复，每次回复控制在2-3句话内。\n\n" +
        "当主人让你执行电脑操作时，请在回复末尾用【命令:xxx】的形式标记，例如：\n" +
        "- 打开应用：【命令:打开微信】\n" +
        "- 调节音量：【命令:音量加大】或【命令:音量减小】或【命令:静音】\n" +
        "- 搜索：【命令:搜索关键词】\n" +
        "- 锁屏：【命令:锁屏】\n" +
        "- 截图：【命令:截图】\n" +
        "- 打开常见应用：【命令:打开浏览器/记事本/计算器/设置】";

    // Knowledge Base
    public string UserName { get; set; } = "主人";
    public string UserProfile { get; set; } = "";

    // Voice Settings (Phase 3)
    public bool VoiceEnabled { get; set; } = false;
    public string WakeWord { get; set; } = "嘿小助手";
    public string SpeechRegion { get; set; } = "eastasia";
    public string SpeechKey { get; set; } = "";
    public double TtsSpeed { get; set; } = 1.0;

    // Appearance
    public double PetScale { get; set; } = 1.0;
    public double PetOpacity { get; set; } = 0.95;
    public double SavedLeft { get; set; } = 0;
    public double SavedTop { get; set; } = 0;

    // General
    public bool StartWithWindows { get; set; } = false;
    public bool StartMinimized { get; set; } = false;
    public string Language { get; set; } = "zh-CN";
}
