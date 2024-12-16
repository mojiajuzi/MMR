using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using System.Threading;
using Avalonia.Controls.Notifications;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using MMR.Components.Popups.AddContact;
using MMR.Components.Popups.AddExpense;
using MMR.ViewModels;
using MMR.Views;
using MMR.Services;

namespace MMR;

public partial class App : Application
{
    public static INotificationManager? NotificationManager { get; set; }
    private IServiceCollection? _services;
    private IServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // 初始化服务容器
        _services = new ServiceCollection();
        RegisterServices();
        _serviceProvider = _services.BuildServiceProvider();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // 初始化默认语言
        LanguageService.Instance.ChangeLanguage("zh-Hans");
        
        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
            {
                DisableAvaloniaDataAnnotationValidation();

                // 使用 DI 创建 MainViewModel
                var mainViewModel = _serviceProvider!.GetRequiredService<MainViewModel>();
                desktop.MainWindow = new MainWindow
                {
                    DataContext = mainViewModel
                };
                NotificationManager = new WindowNotificationManager(desktop.MainWindow);
                break;
            }
            case ISingleViewApplicationLifetime singleViewPlatform:
            {
                var mainViewModel = _serviceProvider!.GetRequiredService<MainViewModel>();
                singleViewPlatform.MainView = new MainView
                {
                    DataContext = mainViewModel
                };
                break;
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    private void RegisterServices()
    {
        _services!.AddSingleton<IDialogService, DialogService>();
        _services!.AddSingleton<LanguageService>();
        // 注册 ViewModels
        _services!.AddTransient<MainViewModel>();
        _services!.AddTransient<TagViewModel>();
        _services!.AddTransient<ContactViewModel>();
        _services!.AddTransient<WorkViewModel>();
        _services!.AddTransient<AddContactViewModel>();
        _services!.AddTransient<AddExpenseViewModel>();
        _services!.AddTransient<DashboardViewModel>();
    }
}