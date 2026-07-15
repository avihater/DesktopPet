using System.Speech.Recognition;
using System.Speech.Synthesis;

namespace DesktopPet.Services;

/// <summary>
/// Voice service using Windows built-in speech recognition and TTS
/// </summary>
public class VoiceService : IDisposable
{
    private readonly ConfigService _config;
    private SpeechRecognitionEngine? _recognizer;
    private SpeechSynthesizer? _synthesizer;

    // Events
    public event Action<string>? SpeechRecognized;
    public event Action? SpeechNotRecognized;      // NEW: fired when nothing was understood
    public event Action<VoiceState>? StateChanged;
    public event Action<string>? SpeakProgress;

    private VoiceState _currentState = VoiceState.Idle;
    public VoiceState CurrentState
    {
        get => _currentState;
        private set
        {
            if (_currentState != value)
            {
                _currentState = value;
                StateChanged?.Invoke(value);
            }
        }
    }

    public bool IsAvailable { get; private set; }
    public string CurrentVoiceName { get; private set; } = "未知";

    public VoiceService(ConfigService config)
    {
        _config = config;
        Initialize();
    }

    private void Initialize()
    {
        try
        {
            // ---- Speech Recognition ----
            _recognizer = new SpeechRecognitionEngine();

            // Use DictationGrammar for general speech recognition
            _recognizer.LoadGrammar(new DictationGrammar());

            _recognizer.SetInputToDefaultAudioDevice();

            _recognizer.SpeechDetected += (s, e) =>
            {
                CurrentState = VoiceState.Listening;
            };

            _recognizer.SpeechRecognized += Recognizer_SpeechRecognized;

            // CRITICAL FIX: handle the case where nothing was recognized
            _recognizer.RecognizeCompleted += (s, e) =>
            {
                // If we're still in Listening state when recognition completes,
                // it means nothing was recognized → tell the UI
                if (CurrentState == VoiceState.Listening)
                {
                    SpeechNotRecognized?.Invoke();
                    CurrentState = VoiceState.Idle;
                }
                // If we're in Processing, the SpeechRecognized handler already fired
                // Don't reset state here - let the AI/TTS flow handle it
            };

            // ---- Speech Synthesis (TTS) ----
            _synthesizer = new SpeechSynthesizer();
            _synthesizer.SetOutputToDefaultAudioDevice();
            _synthesizer.SpeakProgress += (s, e) =>
            {
                SpeakProgress?.Invoke(e.Text);
            };
            _synthesizer.SpeakCompleted += (s, e) =>
            {
                if (CurrentState == VoiceState.Speaking)
                    CurrentState = VoiceState.Idle;
            };

            ConfigureVoice();
            IsAvailable = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Voice init failed: {ex.Message}");
            IsAvailable = false;
        }
    }

    private void ConfigureVoice()
    {
        if (_synthesizer == null) return;

        try
        {
            var allVoices = _synthesizer.GetInstalledVoices();
            var voiceNames = allVoices
                .Select(v => $"{v.VoiceInfo.Name} ({v.VoiceInfo.Culture})")
                .ToList();

            System.Diagnostics.Debug.WriteLine(
                $"Installed TTS voices: {string.Join(", ", voiceNames)}");

            // Try Chinese voice first
            var chineseVoice = allVoices
                .FirstOrDefault(v => v.VoiceInfo.Culture.Name.StartsWith("zh"));
            if (chineseVoice != null)
            {
                _synthesizer.SelectVoice(chineseVoice.VoiceInfo.Name);
                CurrentVoiceName = chineseVoice.VoiceInfo.Name;
                System.Diagnostics.Debug.WriteLine($"Selected voice: {CurrentVoiceName}");
            }
            else if (allVoices.Count > 0)
            {
                // Fallback to first available voice
                _synthesizer.SelectVoice(allVoices[0].VoiceInfo.Name);
                CurrentVoiceName = allVoices[0].VoiceInfo.Name;
                System.Diagnostics.Debug.WriteLine(
                    $"No Chinese voice found, fallback to: {CurrentVoiceName}");
            }
            else
            {
                CurrentVoiceName = "无可用语音";
                System.Diagnostics.Debug.WriteLine("No TTS voices installed!");
            }

            _synthesizer.Rate = (int)((_config.Config.TtsSpeed - 1.0) * 5);
            _synthesizer.Volume = 100;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ConfigureVoice failed: {ex.Message}");
        }
    }

    #region Speech Recognition

    public void StartListening()
    {
        if (_recognizer == null || !IsAvailable) return;

        try
        {
            CurrentState = VoiceState.Listening;
            _recognizer.RecognizeAsync(RecognizeMode.Single);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StartListening failed: {ex.Message}");
            CurrentState = VoiceState.Idle;
        }
    }

    public void CancelListening()
    {
        try
        {
            _recognizer?.RecognizeAsyncCancel();
        }
        catch { }
        CurrentState = VoiceState.Idle;
    }

    private void Recognizer_SpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
    {
        // LOWERED threshold from 0.3 to 0.15 for better pickup
        if (e.Result.Confidence > 0.15 && !string.IsNullOrWhiteSpace(e.Result.Text))
        {
            var text = e.Result.Text.Trim();
            System.Diagnostics.Debug.WriteLine(
                $"Speech recognized (confidence={e.Result.Confidence:F2}): \"{text}\"");

            CurrentState = VoiceState.Processing;
            SpeechRecognized?.Invoke(text);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine(
                $"Speech rejected: confidence={e.Result.Confidence:F2}, text=\"{e.Result.Text}\"");
            // Don't change state here — let RecognizeCompleted handle it
            // This way SpeechNotRecognized will fire
        }
    }

    #endregion

    #region Speech Synthesis (TTS)

    /// <summary>
    /// Speak text using TTS (async, non-blocking).
    /// Returns immediately; SpeakCompleted event fires when done.
    /// </summary>
    public void Speak(string text)
    {
        if (_synthesizer == null || !IsAvailable || string.IsNullOrWhiteSpace(text))
        {
            System.Diagnostics.Debug.WriteLine(
                $"Speak skipped: available={IsAvailable}, text empty={string.IsNullOrWhiteSpace(text)}");
            return;
        }

        try
        {
            // Cancel any ongoing speech first
            _synthesizer.SpeakAsyncCancelAll();

            // Strip emoji/kaomoji from TTS for cleaner speech
            var cleanText = StripKaomoji(text);

            CurrentState = VoiceState.Speaking;

            // SpeakAsync returns immediately, speech plays on background thread
            var prompt = _synthesizer.SpeakAsync(cleanText);

            System.Diagnostics.Debug.WriteLine(
                $"TTS started: \"{cleanText[..Math.Min(50, cleanText.Length)]}...\"");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Speak failed: {ex.Message}");
            CurrentState = VoiceState.Idle;
        }
    }

    public void StopSpeaking()
    {
        try
        {
            _synthesizer?.SpeakAsyncCancelAll();
        }
        catch { }
        CurrentState = VoiceState.Idle;
    }

    /// <summary>
    /// Remove kaomoji and emoji from text for cleaner TTS
    /// </summary>
    private static string StripKaomoji(string text)
    {
        // Remove common kaomoji patterns
        var result = System.Text.RegularExpressions.Regex.Replace(
            text, @"[\(（][\^・ω\*´`;∀\-\.,><@#\$%&!?~\+=\^～\*\)\(\)\[\]]+[\)）]", "");
        // Remove standalone emoji-like symbols
        result = System.Text.RegularExpressions.Regex.Replace(
            result, @"[\uD800-\uDFFF\u2600-\u27BF\u2300-\u23FF\u2B50\u2764]", "");
        return result.Trim();
    }

    #endregion

    public bool IsWakeWordSupported => false;

    public void Dispose()
    {
        try
        {
            _recognizer?.RecognizeAsyncCancel();
            _recognizer?.Dispose();
            _synthesizer?.SpeakAsyncCancelAll();
            _synthesizer?.Dispose();
        }
        catch { }
    }
}

public enum VoiceState
{
    Idle,
    Listening,
    Processing,
    Speaking
}
