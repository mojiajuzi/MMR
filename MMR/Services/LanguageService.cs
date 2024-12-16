using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using MMR.ViewModels;

namespace MMR.Services;

public class LanguageService
{
    private static LanguageService? _instance;
    public static LanguageService Instance => _instance ??= new LanguageService();

    public event EventHandler<CultureInfo>? LanguageChanged;

    public void ChangeLanguage(string cultureName)
    {
        var culture = new CultureInfo(cultureName);
        Lang.Resources.Culture = culture;

        Dispatcher.UIThread.Post(() =>
        {
            LanguageChanged?.Invoke(this, culture);

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;
                if (mainWindow?.DataContext is MainViewModel mainViewModel)
                {
                    // 使用公共方法重新创建当前视图
                    mainViewModel.RecreateCurrentView();
                }
            }
        });
    }

    public bool IsCurrentLanguage(string cultureName)
    {
        return Lang.Resources.Culture?.Name == cultureName;
    }
}