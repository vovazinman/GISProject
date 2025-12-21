using System;
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
    private readonly string _model;

    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private const string DefaultModel = "claude-sonnet-4-20250514";

    public AnthropicClient(string apiKey, string model = DefaultModel)
    {
        _model = model;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        _httpClient.Timeout = TimeSpan.FromSeconds(60); // Timeout
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

        return await SendRequestAsync(request);
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

        return await SendRequestAsync(request);
    }

    /// <summary>
    /// Simple test method - works exactly like the test project.
    /// </summary>
    public async Task<string> SimpleTestAsync(string message)
    {
        // Use anonymous object - exactly like the working test!
        var requestBody = new
        {
            model = _model,
            max_tokens = 1024,
            messages = new[]
            {
            new { role = "user", content = message }
        }
        };

        try
        {

            var response = await _httpClient.PostAsJsonAsync(ApiUrl, requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return $"[ERROR] {response.StatusCode}: {error}";
            }

            var json = await response.Content.ReadAsStringAsync();

            // Simple JSON parsing
            var startIndex = json.IndexOf("\"text\":\"") + 8;
            var endIndex = json.IndexOf("\"", startIndex);

            if (startIndex > 8 && endIndex > startIndex)
            {
                return json.Substring(startIndex, endIndex - startIndex);
            }

            return json; // Return raw if parsing fails
        }
        catch (Exception ex)
        {
            return $"[EXCEPTION] {ex.GetType().Name}: {ex.Message}";
        }
    }

    /// <summary>
    /// Core request method with error handling.
    /// </summary>
    private async Task<string> SendRequestAsync(AnthropicRequest request)
    {
        try
        {
            Console.WriteLine($"[DEBUG] Sending request to: {ApiUrl}");
            Console.WriteLine($"[DEBUG] Model: {request.Model}");
            Console.WriteLine($"[DEBUG] Messages: {request.Messages.Count}");

            var response = await _httpClient.PostAsJsonAsync(ApiUrl, request);

            Console.WriteLine($"[DEBUG] Status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[DEBUG] Error response: {errorContent}");
                throw new HttpRequestException($"API Error {response.StatusCode}: {errorContent}");
            }

            var result = await response.Content.ReadFromJsonAsync<AnthropicResponse>();
            var text = result?.Content?.FirstOrDefault()?.Text ?? string.Empty;

            Console.WriteLine($"[DEBUG] Response length: {text.Length} chars");

            return text;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[ERROR] HTTP Error: {ex.Message}");
            throw;
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine($"[ERROR] Timeout: {ex.Message}");
            throw new Exception("Request timed out. Check your internet connection.");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[ERROR] JSON Parse Error: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] {ex.GetType().Name}: {ex.Message}");
            throw;
        }
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

        HttpResponseMessage response;
        try
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
            {
                Content = JsonContent.Create(request)
            };

            response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Stream request failed: {ex.Message}");
            yield break;
        }

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

            StreamChunk? chunk = null;
            try
            {
                chunk = JsonSerializer.Deserialize<StreamChunk>(data);
            }
            catch { }

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

    [JsonPropertyName("error")]
    public ApiError? Error { get; set; }
}

public class ContentBlock
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

public class ApiError
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
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
