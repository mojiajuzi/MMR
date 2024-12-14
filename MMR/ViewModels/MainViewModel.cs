using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace MMR.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;
    
    [ObservableProperty] private bool _panOpen = true;
    [ObservableProperty] private ViewModelBase _currentView;

    public MainViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        CurrentView = _serviceProvider.GetRequiredService<TagViewModel>();
    }

    [RelayCommand]
    private void PanOpenTrigger()
    {
        PanOpen = !PanOpen;
    }

    public ObservableCollection<ListItemTemplate> Items { get; } = new()
    {
        new ListItemTemplate(typeof(TagViewModel), "Tags", "fa-thin fa-tag"),
        new ListItemTemplate(typeof(ContactViewModel), "Contacts", "fa-thin fa-address-book"),
        new ListItemTemplate(typeof(WorkViewModel), "Works", "fa-thin fa-calendar-days")
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
}