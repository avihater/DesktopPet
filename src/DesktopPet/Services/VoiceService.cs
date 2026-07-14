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

    public VoiceService(ConfigService config)
    {
        _config = config;
        Initialize();
    }

    private void Initialize()
    {
        try
        {
            // Initialize speech recognition
            _recognizer = new SpeechRecognitionEngine();
            var dictationGrammar = new DictationGrammar();
            _recognizer.LoadGrammar(dictationGrammar);
            _recognizer.SetInputToDefaultAudioDevice();
            _recognizer.SpeechRecognized += Recognizer_SpeechRecognized;
            _recognizer.SpeechDetected += (s, e) => CurrentState = VoiceState.Listening;
            _recognizer.RecognizeCompleted += (s, e) =>
            {
                if (CurrentState == VoiceState.Listening)
                    CurrentState = VoiceState.Idle;
            };

            // Initialize speech synthesis
            _synthesizer = new SpeechSynthesizer();
            _synthesizer.SetOutputToDefaultAudioDevice();
            _synthesizer.SpeakProgress += Synthesizer_SpeakProgress;
            _synthesizer.SpeakCompleted += (s, e) =>
            {
                if (CurrentState == VoiceState.Speaking)
                    CurrentState = VoiceState.Idle;
            };

            // Configure voice
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
            // Try to find a Chinese voice
            var voices = _synthesizer.GetInstalledVoices();
            var chineseVoice = voices
                .FirstOrDefault(v => v.VoiceInfo.Culture.Name.StartsWith("zh"));

            if (chineseVoice != null)
            {
                _synthesizer.SelectVoice(chineseVoice.VoiceInfo.Name);
            }

            _synthesizer.Rate = (int)((_config.Config.TtsSpeed - 1.0) * 5);
            _synthesizer.Volume = 100;
        }
        catch { }
    }

    #region Speech Recognition

    /// <summary>
    /// Start listening for speech (one-shot)
    /// </summary>
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

    /// <summary>
    /// Cancel current listening
    /// </summary>
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
        if (e.Result.Confidence > 0.3 && !string.IsNullOrWhiteSpace(e.Result.Text))
        {
            var text = e.Result.Text.Trim();
            CurrentState = VoiceState.Processing;
            SpeechRecognized?.Invoke(text);
        }
        else
        {
            CurrentState = VoiceState.Idle;
        }
    }

    #endregion

    #region Speech Synthesis (TTS)

    /// <summary>
    /// Speak text using TTS (async, non-blocking)
    /// </summary>
    public void Speak(string text)
    {
        if (_synthesizer == null || !IsAvailable || string.IsNullOrWhiteSpace(text))
            return;

        try
        {
            // Cancel any ongoing speech
            _synthesizer.SpeakAsyncCancelAll();

            CurrentState = VoiceState.Speaking;
            _synthesizer.SpeakAsync(text);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Speak failed: {ex.Message}");
            CurrentState = VoiceState.Idle;
        }
    }

    /// <summary>
    /// Stop speaking immediately
    /// </summary>
    public void StopSpeaking()
    {
        try
        {
            _synthesizer?.SpeakAsyncCancelAll();
        }
        catch { }
        CurrentState = VoiceState.Idle;
    }

    private void Synthesizer_SpeakProgress(object? sender, SpeakProgressEventArgs e)
    {
        SpeakProgress?.Invoke(e.Text);
    }

    #endregion

    #region Wake Word Simulation

    // Since System.Speech doesn't support keyword spotting natively,
    // we use a keyboard shortcut as the primary trigger.
    // Continuous listening with keyword detection would require
    // Azure Speech SDK or a more advanced solution.

    /// <summary>
    /// Simulates wake word detection - always returns true
    /// (Real implementation requires Azure Speech SDK keyword recognition)
    /// </summary>
    public bool IsWakeWordSupported => false;

    #endregion

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

/// <summary>
/// Voice interaction state machine
/// </summary>
public enum VoiceState
{
    Idle,       // Not doing anything
    Listening,  // Waiting for speech input
    Processing, // Processing recognized speech
    Speaking    // Speaking TTS output
}
