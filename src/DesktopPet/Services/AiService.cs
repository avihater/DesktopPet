using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DesktopPet.Models;

namespace DesktopPet.Services;

/// <summary>
/// AI service for DeepSeek (OpenAI-compatible) API integration
/// </summary>
public class AiService
{
    private readonly ConfigService _config;
    private readonly HttpClient _http;
    private readonly List<ChatMessage> _history = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public AiService(ConfigService config)
    {
        _config = config;
        _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60)
        };
    }

    /// <summary>
    /// Get conversation history
    /// </summary>
    public IReadOnlyList<ChatMessage> History => _history.AsReadOnly();

    /// <summary>
    /// Send a user message and get streaming response
    /// </summary>
    public async IAsyncEnumerable<string> ChatStreamAsync(string userMessage)
    {
        var cfg = _config.Config;

        // Add user message to history
        var userMsg = new ChatMessage { Role = "user", Content = userMessage };
        _history.Add(userMsg);

        // Trim history to max rounds
        TrimHistory(cfg.MaxHistoryRounds);

        // Build messages array for API
        var messages = new List<object>();
        messages.Add(new { role = "system", content = _config.GetSystemPrompt() });

        foreach (var msg in _history.TakeLast(cfg.MaxHistoryRounds * 2))
        {
            messages.Add(new { role = msg.Role, content = msg.Content });
        }

        // Create request
        var requestBody = new
        {
            model = cfg.ModelName,
            messages,
            temperature = cfg.Temperature,
            stream = true,
            max_tokens = 1024
        };

        var requestJson = JsonSerializer.Serialize(requestBody, JsonOptions);
        var httpContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post,
            $"{cfg.ApiBaseUrl.TrimEnd('/')}/chat/completions")
        {
            Content = httpContent
        };
        request.Headers.Add("Authorization", $"Bearer {cfg.ApiKey}");
        request.Headers.Add("Accept", "text/event-stream");

        // Create assistant message placeholder
        var assistantMsg = new ChatMessage { Role = "assistant", Content = "", IsStreaming = true };
        _history.Add(assistantMsg);

        // Collect errors outside try/catch for yielding
        string? errorMessage = null;
        HttpResponseMessage? response = null;

        try
        {
            response = await _http.SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            errorMessage = $"抱歉主人，连接失败了呢~ {ex.Message} (´;ω;`)";
        }

        // If error, yield the error message
        if (errorMessage != null)
        {
            assistantMsg.Content = errorMessage;
            assistantMsg.IsStreaming = false;
            yield return errorMessage;
            yield break;
        }

        // Stream the response (no yield in catch)
        using var stream = await response!.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (!line.StartsWith("data: ")) continue;

            var data = line["data: ".Length..];
            if (data == "[DONE]") break;

            string? chunk = null;
            try
            {
                using var doc = JsonDocument.Parse(data);
                var choices = doc.RootElement.GetProperty("choices");
                if (choices.GetArrayLength() > 0)
                {
                    var delta = choices[0].GetProperty("delta");
                    if (delta.TryGetProperty("content", out var contentEl))
                    {
                        chunk = contentEl.GetString();
                    }
                }
            }
            catch { /* skip malformed chunks */ }

            if (!string.IsNullOrEmpty(chunk))
            {
                assistantMsg.Content += chunk;
                yield return chunk;
            }
        }

        assistantMsg.IsStreaming = false;
    }

    /// <summary>
    /// Clear conversation history
    /// </summary>
    public void ClearHistory()
    {
        _history.Clear();
    }

    private void TrimHistory(int maxRounds)
    {
        // Keep only system prompt context + last N rounds
        // Each round = user + assistant = 2 messages
        int maxMessages = maxRounds * 2;
        while (_history.Count > maxMessages)
        {
            _history.RemoveAt(0);
        }
    }
}
