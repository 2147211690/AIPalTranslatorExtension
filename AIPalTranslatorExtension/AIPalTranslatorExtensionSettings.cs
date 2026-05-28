using System;
using System.IO;
using AIPalTranslatorExtension.Helper;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AiTranslatorExtension;

public class AIPalTranslatorExtensionSettings : JsonSettingsManager
{
    public static readonly AIPalTranslatorExtensionSettings Instance = new();
    public Settings Settings { get; }
    public AIPalTranslatorExtensionSettings()
    {
        FilePath = GetSettingsPath();
        Settings = new Settings();
        LoadSettings();
        Settings.Add(ModelUrl);
        Settings.Add(Model);
        Settings.Add(ApiKey);
        Settings.Add(Language1);
        Settings.Add(Language2);
        Settings.Add(MaxOutputToken);
        Settings.Add(MoreRule);
        Settings.SettingsChanged += SettingsOnSettingsChanged;
    }

    private void SettingsOnSettingsChanged(object sender, Settings args)
    {
        TranslateHelper.ModelUrl = ModelUrl.Value ?? string.Empty;
        TranslateHelper.Model = Model.Value ?? string.Empty;
        TranslateHelper.ApiKey = ApiKey.Value ?? string.Empty;
        TranslateHelper.Language1 = Language1.Value ?? string.Empty;
        TranslateHelper.Language2 = Language2.Value ?? string.Empty;
        TranslateHelper.MoreRule = MoreRule.Value?.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        TranslateHelper.MaxOutputToken = 
            int.TryParse(MaxOutputToken.Value, out var maxOutputToken) ? maxOutputToken : 50;
        SaveSettings();
    }

    public readonly TextSetting ModelUrl = new("ModelUrl", "Model URL", "您的模型URI", "https://api.deepseek.com")
    {
        Placeholder = "https://api.deepseek.com"
    };
    public readonly TextSetting Model = new("Model", "Model", "您的模型", "deepseek-v4-flash")
    {
        Placeholder = "deepseek-v4-flash"
    };
    public readonly TextSetting ApiKey = new("ApiKey", "API Key", "您的API Key", "")
    {
        Placeholder = "请输入您的API Key"
    };
    public readonly TextSetting Language1 = new("Language1", "Language 1", "目标语言", "zh-cn")
    {
        Placeholder = "zh-cn"
    };
    public readonly TextSetting Language2 = new("Language2", "Language 2", "源语言", "en-us")
    {
        Placeholder = "en-us"
    };
    public readonly TextSetting MaxOutputToken = new("MaxOutputToken", "Max Output Token", "最大输出Token", "30")
    {
        Placeholder = "30"
    };

    public readonly TextSetting MoreRule = new("MoreRule", "More Rule", "添加更多提示规则(通常使用英文提示,使用换行隔开)", 
        """
        Proper nouns: use standard name first (e.g. 我的世界 → minecraft). literal as alternative.
        Use lowercase whenever possible.
        Output 1-5 translations per input.
        """)
    {
        Multiline = true,
        Placeholder = "Use lowercase whenever possible.;Output 1-5 translations per input.",
    };
    internal static string GetSettingsPath()
    {
        var directory = Utilities.BaseSettingsPath("Noper.AiTranslator");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "settings.json");
    }
}