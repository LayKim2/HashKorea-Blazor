using HashKorea.Common.Constants;
using HashKorea.Data;
using HashKorea.DTOs.Auth;
using HashKorea.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace HashKorea.Services;

public class OpenAIService : IOpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public OpenAIService()
    {
        _httpClient = new HttpClient();
        _apiKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY") ?? string.Empty;

        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://your-site.com"); // Optional
        _httpClient.DefaultRequestHeaders.Add("X-Title", "Your Site Name"); // Optional
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<string> GetChatGPTResponse(string prompt)
    {
        var requestBody = new
        {
            model = "qwen/qwen-vl-plus:free",
            messages = new[]
            {
                new { role = "system", content = "You are a helpful assistant." },
                new { role = "user", content = prompt }
            },
        };

        var jsonRequest = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://openrouter.ai/api/v1/chat/completions", content);

        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return $"Error: {response.StatusCode}, {responseBody}";
        }

        using var doc = JsonDocument.Parse(responseBody);
        var reply = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

        return reply ?? "No response from DeepSeek AI.";
    }

}