using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MMR.Services;

namespace MMR.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty] private bool _panOpen = true;
    [ObservableProperty] private ViewModelBase _currentView;

    public MainViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        CurrentView = _serviceProvider.GetRequiredService<DashboardViewModel>();

        // 订阅语言变化事件
        LanguageService.Instance.LanguageChanged += (_, _) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                // 保存当前视图的类型
                var currentViewType = CurrentView.GetType();

                // 重新创建当前视图
                CurrentView = (ViewModelBase)_serviceProvider.GetRequiredService(currentViewType);

                // 刷新菜单项列表
                Items.Clear();
                Items.Add(new ListItemTemplate(typeof(DashboardViewModel), Lang.Resources.Dashboard,
                    "fa-solid fa-chart-line"));
                Items.Add(new ListItemTemplate(typeof(TagViewModel), Lang.Resources.Tag, "fa-solid fa-tag"));
                Items.Add(new ListItemTemplate(typeof(ContactViewModel), Lang.Resources.Contact,
                    "fa-solid fa-address-book"));
                Items.Add(new ListItemTemplate(typeof(WorkViewModel), Lang.Resources.Work,
                    "fa-solid fa-calendar-days"));

                // 找到并重新设置选中项
                var selectedItem = Items.FirstOrDefault(i => i.ViewModelType == currentViewType);
                if (selectedItem != null)
                {
                    SelectedItem = selectedItem;
                }
            });
        };
    }

    [RelayCommand]
    private void PanOpenTrigger()
    {
        PanOpen = !PanOpen;
    }

    public ObservableCollection<ListItemTemplate> Items { get; } = new()
    {
        new ListItemTemplate(typeof(DashboardViewModel), Lang.Resources.Dashboard, "fa-solid fa-chart-line"),
        new ListItemTemplate(typeof(TagViewModel), Lang.Resources.Tag, "fa-solid fa-tag"),
        new ListItemTemplate(typeof(ContactViewModel), Lang.Resources.Contact, "fa-solid fa-address-book"),
        new ListItemTemplate(typeof(WorkViewModel), Lang.Resources.Work, "fa-solid fa-calendar-days")
    };

    [ObservableProperty] private ListItemTemplate _selectedItem;

    partial void OnSelectedItemChanged(ListItemTemplate? value)
    {
        if (value is null) return;
        CurrentView = (ViewModelBase)_serviceProvider.GetRequiredService(value.ViewModelType);
    }

    public class ListItemTemplate
    {
        public ListItemTemplate(Type viewModelType, string viewModelName, string iconKey)
        {
            ViewModelType = viewModelType;
            ViewModelName = viewModelName;
            Icon = iconKey;
        }

        public Type ViewModelType { get; set; }
        public string ViewModelName { get; set; }
        public string Icon { get; set; }
    }

    // 添加公共方法用于重新创建视图
    public void RecreateCurrentView()
    {
        var currentViewType = CurrentView.GetType();
        CurrentView = (ViewModelBase)_serviceProvider.GetRequiredService(currentViewType);
    }
}