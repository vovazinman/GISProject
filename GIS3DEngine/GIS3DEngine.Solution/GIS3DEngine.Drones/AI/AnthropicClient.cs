using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GIS3DEngine.Drones.AI;

/// <summary>
/// Client for Anthropic Claude API.
/// </summary>
public class AnthropicClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;

    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private const string DefaultModel = "claude-sonnet-4-20250514";

    public AnthropicClient(string apiKey, string model = DefaultModel)
    {
        _apiKey = apiKey;
        _model = model;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    /// <summary>
    /// Send a message to Claude and get a response.
    /// </summary>
    public async Task<string> SendMessageAsync(string userMessage, string? systemPrompt = null)
    {
        var request = new AnthropicRequest
        {
            Model = _model,
            MaxTokens = 1024,
            System = systemPrompt,
            Messages = new List<Message>
            {
                new() { Role = "user", Content = userMessage }
            }
        };

        var response = await _httpClient.PostAsJsonAsync(ApiUrl, request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AnthropicResponse>();
        return result?.Content?.FirstOrDefault()?.Text ?? string.Empty;
    }

    /// <summary>
    /// Send a conversation to Claude.
    /// </summary>
    public async Task<string> SendConversationAsync(
        List<Message> messages,
        string? systemPrompt = null)
    {
        var request = new AnthropicRequest
        {
            Model = _model,
            MaxTokens = 1024,
            System = systemPrompt,
            Messages = messages
        };

        var response = await _httpClient.PostAsJsonAsync(ApiUrl, request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AnthropicResponse>();
        return result?.Content?.FirstOrDefault()?.Text ?? string.Empty;
    }

    /// <summary>
    /// Stream a response from Claude.
    /// </summary>
    public async IAsyncEnumerable<string> StreamMessageAsync(
        string userMessage,
        string? systemPrompt = null)
    {
        var request = new AnthropicRequest
        {
            Model = _model,
            MaxTokens = 1024,
            System = systemPrompt,
            Stream = true,
            Messages = new List<Message>
            {
                new() { Role = "user", Content = userMessage }
            }
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
        {
            Content = JsonContent.Create(request)
        };

        var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(line) || !line.StartsWith("data: "))
                continue;

            var data = line[6..];
            if (data == "[DONE]")
                break;

            var chunk = JsonSerializer.Deserialize<StreamChunk>(data);
            if (chunk?.Delta?.Text != null)
                yield return chunk.Delta.Text;
        }
    }
}

#region API Models

public class AnthropicRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 1024;

    [JsonPropertyName("system")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? System { get; set; }

    [JsonPropertyName("messages")]
    public List<Message> Messages { get; set; } = new();

    [JsonPropertyName("stream")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Stream { get; set; }
}

public class Message
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public class AnthropicResponse
{
    [JsonPropertyName("content")]
    public List<ContentBlock>? Content { get; set; }

    [JsonPropertyName("stop_reason")]
    public string? StopReason { get; set; }
}

public class ContentBlock
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

public class StreamChunk
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("delta")]
    public Delta? Delta { get; set; }
}

public class Delta
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

#endregion