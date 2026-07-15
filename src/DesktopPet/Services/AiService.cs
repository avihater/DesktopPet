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

    // Default timeout for chat requests
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

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
            Timeout = TimeSpan.FromSeconds(45) // Total HTTP timeout (including streaming)
        };
    }

    public IReadOnlyList<ChatMessage> History => _history.AsReadOnly();

    /// <summary>
    /// Send a user message and get streaming response.
    /// </summary>
    /// <param name="userMessage">User's message text</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    public async IAsyncEnumerable<string> ChatStreamAsync(
        string userMessage,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        var cfg = _config.Config;

        // Validate API key
        if (string.IsNullOrWhiteSpace(cfg.ApiKey))
        {
            yield return "主人还没设置 API Key 呢~ 请先在设置中配置 DeepSeek 的 API Key！(◕‿◕)";
            yield break;
        }

        // Add user message to history
        var userMsg = new ChatMessage { Role = "user", Content = userMessage };
        _history.Add(userMsg);
        TrimHistory(cfg.MaxHistoryRounds);

        // Build messages array
        var messages = new List<object>
        {
            new { role = "system", content = _config.GetSystemPrompt() }
        };

        foreach (var msg in _history.TakeLast(cfg.MaxHistoryRounds * 2))
        {
            messages.Add(new { role = msg.Role, content = msg.Content });
        }

        // Build request
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

        // Create a linked cancellation token with timeout
        using var timeoutCts = new CancellationTokenSource(RequestTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);

        // Send request (with error collection outside try/catch for yield safety)
        string? errorMessage = null;
        HttpResponseMessage? response = null;

        try
        {
            response = await _http.SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead,
                linkedCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                errorMessage = "请求超时了~ 网络可能不太好，主人稍后再试试？(´・ω・`)";
            else
                errorMessage = "请求被取消了哦~";
        }
        catch (HttpRequestException ex)
        {
            errorMessage = $"网络连接失败：{ex.Message} (´;ω;`)\n请检查 API 地址和网络连接~";
        }
        catch (Exception ex)
        {
            errorMessage = $"出错了：{ex.Message} (´;ω;`)";
        }

        if (errorMessage != null)
        {
            assistantMsg.Content = errorMessage;
            assistantMsg.IsStreaming = false;
            yield return errorMessage;
            yield break;
        }

        // Check HTTP status
        if (response is { IsSuccessStatusCode: false })
        {
            var statusCode = (int)response.StatusCode;
            string statusMsg = statusCode switch
            {
                401 => "API Key 无效，请检查设置中的 Key 是否正确~",
                402 => "API 余额不足，请充值后再试~",
                429 => "请求太频繁了，稍等一下再试吧~",
                500 => "服务器出了点问题，稍后再试~",
                _ => $"服务器返回错误 ({statusCode})，请稍后再试~"
            };
            assistantMsg.Content = statusMsg;
            assistantMsg.IsStreaming = false;
            yield return statusMsg;
            yield break;
        }

        // Stream the response
        using var stream = await response!.Content.ReadAsStreamAsync(linkedCts.Token)
            .ConfigureAwait(false);
        using var reader = new StreamReader(stream);
        var receivedAnyChunk = false;

        while (!reader.EndOfStream)
        {
            linkedCts.Token.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(linkedCts.Token).ConfigureAwait(false);

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
            catch { /* skip malformed SSE chunks */ }

            if (!string.IsNullOrEmpty(chunk))
            {
                receivedAnyChunk = true;
                assistantMsg.Content += chunk;
                yield return chunk;
            }
        }

        // If we received nothing, something went wrong
        if (!receivedAnyChunk)
        {
            var msg = "唔…AI 没有返回内容呢，可能是模型繁忙，再试一次？(°◇°;)";
            assistantMsg.Content += msg;
            yield return msg;
        }

        assistantMsg.IsStreaming = false;
    }

    public void ClearHistory()
    {
        _history.Clear();
    }

    private void TrimHistory(int maxRounds)
    {
        int maxMessages = maxRounds * 2;
        while (_history.Count > maxMessages)
            _history.RemoveAt(0);
    }
}
