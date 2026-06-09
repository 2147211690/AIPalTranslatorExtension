using System;
using System.IO;
using System.Linq;
using AIPalTranslatorExtension.Helper;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AIPalTranslatorExtension;

public class SettingsManager : JsonSettingsManager
{
    public static readonly SettingsManager Instance = new();
    public string ApiKeyValue { get; private set; } = string.Empty;
    public string ModelValue { get; private set; } = "deepseek-v4-flash";
    public string ModelUrlValue { get; private set; } = "https://api.deepseek.com";
    public string Language1Value { get; private set; } = "zh-cn";
    public string Language2Value { get; private set; } = "en-us";
    public int MaxOutputTokenValue { get; private set; } = 30;
    public bool IsThinkingValue { get; private set; } = false;
    public string SystemMessageValue { get; private set; } = string.Empty;
    public SettingsManager()
    {
        FilePath = GetSettingsPath();
        Settings.Add(_modelUrl);
        Settings.Add(_model);
        Settings.Add(_apiKey);
        Settings.Add(_language1);
        Settings.Add(_language2);
        Settings.Add(_maxOutputToken);
        Settings.Add(_moreRule);
        Settings.Add(_isThinking);
        LoadSettings();
        Settings.SettingsChanged += SettingsOnSettingsChanged;
        SettingsOnSettingsChanged(this, Settings);
    }

    private void SettingsOnSettingsChanged(object sender, Settings args)
    {
        ModelUrlValue = _modelUrl.Value ?? string.Empty;
        ModelValue = _model.Value ?? string.Empty;
        ApiKeyValue = _apiKey.Value ?? string.Empty;
        Language1Value = _language1.Value ?? string.Empty;
        Language2Value = _language2.Value ?? string.Empty;
        MaxOutputTokenValue = 
            int.TryParse(_maxOutputToken.Value, out var maxOutputToken) ? maxOutputToken : 50;
        IsThinkingValue = _isThinking.Value;
        UpdateSystemMessage();
        SaveSettings();
    }
    private void UpdateSystemMessage()
    {
        SystemMessageValue = 
             $"""
              你是一个纯翻译器
              按照以下格式和要求翻译
              用户:(lang1=lang2)<text>
              你:(targetLang)<result>[|alternatives]
              双向翻译text,若非其一则译为lang1
              {_moreRule.Value}
              示例:
              用户:(en-us=zh-cn)Hello
              你:(zh-cn)你好|哈喽|嗨
              
              用户:(en-us=zh-cn)你好
              你:(en-us)hello
              
              用户:(es=it)Hello
              你:(es)hola|ciao
              """;
    }
    private readonly TextSetting _modelUrl = new("ModelUrl", "Model URL", "您的模型URI", "https://api.deepseek.com")
    {
        Placeholder = "https://api.deepseek.com"
    };

    private readonly TextSetting _model = new("Model", "Model", "您的模型", "deepseek-v4-flash")
    {
        Placeholder = "deepseek-v4-flash"
    };

    private readonly TextSetting _apiKey = new("ApiKey", "API Key", "您的API Key", "")
    {
        Placeholder = "请输入您的API Key"
    };

    private readonly TextSetting _language1 = new("Language1", "Language 1", "目标语言", "zh-cn")
    {
        Placeholder = "zh-cn"
    };

    private readonly TextSetting _language2 = new("Language2", "Language 2", "源语言", "en-us")
    {
        Placeholder = "en-us"
    };

    private readonly TextSetting _maxOutputToken = new("MaxOutputToken", "Max Output Token", "最大输出Token", "30")
    {
        Placeholder = "30"
    };

    private readonly TextSetting _moreRule = new("MoreRule", "More Rule", "添加更多提示规则", 
        """
        优先翻译为专有名词
        1到5个翻译结果
        """)
    {
        Multiline = true,
    };
    
    private readonly ToggleSetting _isThinking = new("IsThinking", "Is Thinking", "是否思考", false);
    private static string GetSettingsPath()
    {
        var directory = Utilities.BaseSettingsPath("Noper.AiPalTranslator");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "settings.json");
    }
}