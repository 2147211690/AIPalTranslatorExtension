using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;

namespace AIPalTranslatorExtension.Helper;

public static partial class TranslateHelper
{
    [GeneratedRegex("^\\((?<language>[a-zA-Z-]+?)\\)(?<translations>[^|]+(?:\\|[^|]+)*)$")]
    private static partial Regex ResponseRegex { get; }
    private static SettingsManager S => SettingsManager.Instance;

    public static async Task<TranslateResult> TranslateAsync(string text, CancellationTokenSource cancellationToken)
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
        }).WaitAsync(cancellationToken.Token);
        if (result is null) throw new TaskCanceledException();
        var response = result.Choices[0].Message;
        var match = ResponseRegex.Match(response.Content);
        return !match.Success
            ? throw new InvaildAiResponseException()
            : new TranslateResult(match.Groups["language"].Value,
                match.Groups["translations"].Value.Split('|'),
                response.GetMessageTokens()
            );
    }
}
public record struct TranslateResult(string? ResultLanguage, string[]? ResultTexts, int TokenUsed, Exception? Error = null);
public class InvaildAiResponseException() : Exception("Invalid ai response");