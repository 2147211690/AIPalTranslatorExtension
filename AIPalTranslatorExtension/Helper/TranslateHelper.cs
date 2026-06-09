using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AIPalTranslatorExtension.Helper;

public static partial class TranslateHelper
{
    [GeneratedRegex("^\\((?<language>[a-zA-Z-]+?)\\)(?<translations>[^|]+(?:\\|[^|]+)*)$")]
    private static partial Regex ResponseRegex { get; }

    private static SettingsManager S => SettingsManager.Instance;

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public static async Task<TranslateResult> TranslateAsync(string text, CancellationToken cancellationToken)
    {
        var input = $"({S.Language1Value}={S.Language2Value}){text}";
        var requestBody = new
        {
            model = S.ModelValue,
            messages = new[]
            {
                new { role = "system", content = S.SystemMessageValue },
                new { role = "user", content = input }
            },
            max_tokens = S.MaxOutputTokenValue,
            temperature = 0.1f,
            stream = false,
            thinking = new { type = S.IsThinkingValue ? "enabled" : "disabled" }
        };

        var json = JsonSerializer.Serialize(requestBody);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{S.ModelUrlValue.TrimEnd('/')}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", S.ApiKeyValue);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<DeepSeekResponse>(responseJson);

        if (result?.Choices is not { Length: > 0 })
        {
            throw new InvalidAiResponseException();
        }

        var content = result.Choices[0].Message?.Content ?? string.Empty;
        var match = ResponseRegex.Match(content);
        return !match.Success
            ? throw new InvalidAiResponseException()
            : new TranslateResult(
                match.Groups["language"].Value,
                match.Groups["translations"].Value.Split('|'),
                result.Usage?.TotalTokens ?? -1
            );
    }
}

public class DeepSeekResponse
{
    [JsonPropertyName("choices")]
    public DeepSeekChoice[]? Choices { get; set; }

    [JsonPropertyName("usage")]
    public DeepSeekUsage? Usage { get; set; }
}

public class DeepSeekChoice
{
    [JsonPropertyName("message")]
    public DeepSeekMessage? Message { get; set; }
}

public class DeepSeekMessage
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

public class DeepSeekUsage
{
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}

public record struct TranslateResult(
    string? ResultLanguage,
    string[]? ResultTexts,
    int TokenUsed,
    Exception? Error = null);

public class InvalidAiResponseException() : Exception("Invalid ai response");