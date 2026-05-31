// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using AIPalTranslatorExtension.Helper;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Timer = System.Timers.Timer;
using TranslateHelper = AIPalTranslatorExtension.Helper.TranslateHelper;

namespace AIPalTranslatorExtension.Pages;

internal sealed partial class AIPalTranslatorExtensionPage : DynamicListPage
{
    private Timer _currentTimer;
    private CancellationTokenSource? _cancellationTokenSource = new();
    private TranslateResult _currentResult = new(null, null, 0);
    private Lock _resultLock = new();
    private Lock _cancelLock = new();
    private Lock _timerLock = new();
    
    public static readonly int SearchDelay = 300;
    public AIPalTranslatorExtensionPage()
    {
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = "AI Pal Translator Extension for command palette";
        Name = "Open";
        _currentTimer = new Timer(SearchDelay);
        _currentTimer.AutoReset = false;
        _currentTimer.Elapsed += CurrentTimerOnElapsed;
    }

    private async void CurrentTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            lock (_cancelLock)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = new();
                IsLoading = true;
            }
            var result = await TranslateHelper.TranslateAsync(SearchText, _cancellationTokenSource!);
            lock (_resultLock)
            {
                _currentResult = result;
                RaiseItemsChanged();
                IsLoading = false;
            }
        }
        catch (Exception ex)
        {
            if (ex is not TaskCanceledException)
            {
                _currentResult = new(null, null, 0, ex);
                RaiseItemsChanged();
                IsLoading = false;
            }
        }
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        if (oldSearch == newSearch || string.IsNullOrEmpty(newSearch)) return;
        _currentTimer.Stop();
        _currentTimer.Start();
    }

    public override IListItem[] GetItems()
    {
        if (_currentResult.Error != null) 
            return [GetResultCommand($"错误:{_currentResult.Error}")];
        return _currentResult.ResultTexts?
            .Select(GetResultCommand)
            //.Append(new ListItem(new NoOpCommand()){Title = $"消耗Token:{_currentResult.TokenUsed}"})
            .ToArray() ?? [];
    }

    private IListItem GetResultCommand(string resultText)
    {
        return new ListItem(new CopyTextCommand(resultText))
        {
            Title = resultText,
        };
    }
    ~AIPalTranslatorExtensionPage()
    {
        _currentTimer.Stop();
        _currentTimer.Dispose();
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
    }
}
