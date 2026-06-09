// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AIPalTranslatorExtension.Helper;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using TranslateHelper = AIPalTranslatorExtension.Helper.TranslateHelper;

namespace AIPalTranslatorExtension.Pages;

internal sealed class AIPalTranslatorExtensionPage : DynamicListPage, IDisposable
{
    private CancellationTokenSource? _cancellationTokenSource = new();
    private TranslateResult _currentResult = new(null, null, 0);
    private Lock _resultLock = new();
    private Lock _cancelLock = new();
    private string? _targetLanguage;
    private IListItem[]? _lastResultItems;
    private PageState _state = PageState.Input;
    private string Lang1 => SettingsManager.Instance.Language1Value;
    private string Lang2 => SettingsManager.Instance.Language2Value;
    private PageState State
    {
        get => _state;
        set
        {
            if (_state == value) return;
            // 退出状态
            switch (_state)
            {
                case PageState.Loading:
                    IsLoading = false;
                    break;
            }
            _state = value;
            // 进入状态
            switch (_state)
            {
                case PageState.Input:
                    _cancellationTokenSource?.Cancel();
                    _cancellationTokenSource = null;
                    RaiseItemsChanged();
                    IsLoading = false;
                    break;
                case PageState.Loading:
                    RaiseItemsChanged();
                    IsLoading = true;
                    TranslateAsync();
                    break;
                case PageState.Result:
                    RaiseItemsChanged();
                    break;
            }
        }
    }
    public AIPalTranslatorExtensionPage()
    {
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = "AI Pal Translator Extension for command palette";
        Name = "Open";
    }

    private async void TranslateAsync()
    {
        try
        {
            lock (_cancelLock)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = new();
            }
            var result = await TranslateHelper.TranslateAsync(SearchText, _targetLanguage, _cancellationTokenSource.Token);
            lock (_resultLock)
            {
                _currentResult = result;
                State = PageState.Result;
            }
        }
        catch (Exception ex)
        {
            if (ex is not TaskCanceledException)
            {
                _currentResult = new(null, null, 0, ex);
                State = PageState.Result;
            }
        }
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        State = PageState.Input;
    }

    public override IListItem[] GetItems()
    {
        switch (State)
        {
            case PageState.Input:
                return
                [
                    new ListItem(new TranslateCommand(this)) { Title = "双向翻译" },
                    new ListItem(new TranslateCommand(this, Lang1)) { Title = $"翻译为{Lang1}" },
                    new ListItem(new TranslateCommand(this, Lang2)) { Title = $"翻译为{Lang2}" },
                    .. (_lastResultItems ?? [])
                ];
            case PageState.Result:
                if (_currentResult.Error != null) return _lastResultItems = [GetResultCommand($"错误:{_currentResult.Error}")];
                return _lastResultItems = _currentResult.ResultTexts?
                    .Select(GetResultCommand)
                    .Append(new ListItem(new NoOpCommand()){Title = $"消耗Token:{_currentResult.TokenUsed}"})
                    .ToArray() ?? [];
            default:
                return [];
        }
    }
    
    private IListItem GetResultCommand(string resultText)
    {
        return new ListItem(new CopyTextCommand(resultText))
        {
            Title = resultText,
        };
    }
    
    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cancellationTokenSource?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
    }
    
    private class TranslateCommand(AIPalTranslatorExtensionPage page, string? targetLanguage = null) : InvokableCommand
    {
        private readonly AIPalTranslatorExtensionPage _page = page ?? throw new ArgumentNullException(nameof(page));
        private static readonly CommandResult Result = CommandResult.KeepOpen();
        public override ICommandResult Invoke()
        {
            _page._targetLanguage = targetLanguage;
            _page.State = PageState.Loading;
            return Result;
        }
    }
    
    private enum PageState
    {
        Input,
        Loading,
        Result,
    }
    

}
