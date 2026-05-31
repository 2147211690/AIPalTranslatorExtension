using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Code;
using LlmTornado.Completions;

namespace AIPalTranslatorExtension.Helper;

public static partial class TranslateHelper
{
    [GeneratedRegex("^\\((?<language>[a-zA-Z-]+?)\\)(?<translations>[^|]+(?:\\|[^|]+)*)$")]
    private static partial Regex ResponseRegex { get; }
    private static SettingsManager S => SettingsManager.Instance;

    public static async Task<TranslateResult> TranslateAsync(string text, CancellationToken cancellationToken)
    {
        var input = $"({S.Language1Value}={S.Language2Value}){text}";
        var api = new TornadoApi(new Uri(S.ModelUrlValue), S.ApiKeyValue);
        var model = new ChatModel(S.ModelValue);
        var result = await api.Chat.CreateChatCompletion(new ChatRequest
        {
            Model = model,
            Messages =
            [
                new ChatMessage(ChatMessageRoles.System, S.SystemMessageValue),
                new ChatMessage(ChatMessageRoles.User, input)
            ],
            MaxTokens = S.MaxOutputTokenValue,
            Temperature = 0.1f,
            CancellationToken = cancellationToken,
            VendorExtensions = new ChatRequestVendorExtensions(new ChatRequestVendorAnthropicExtensions()
            {
                Thinking = new AnthropicThinkingSettings()
                {
                    Enabled = false,
                }
            })
        }).WaitAsync(cancellationToken);
        if (result is null) throw new TaskCanceledException();
        var response = result.Choices[0].Message;
        var match = ResponseRegex.Match(response.Content);
        return !match.Success
            ? throw new InvalidAiResponseException()
            : new TranslateResult(match.Groups["language"].Value,
                match.Groups["translations"].Value.Split('|'),
                result.Usage?.CompletionTokens ?? -1
            );
    }
}
public record struct TranslateResult(string? ResultLanguage, string[]? ResultTexts, int TokenUsed, Exception? Error = null);
public class InvalidAiResponseException() : Exception("Invalid ai response");