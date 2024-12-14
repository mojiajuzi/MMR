using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MMR.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private bool _panOpen = true;

    [ObservableProperty] private ViewModelBase _currentView;

    public MainViewModel()
    {
        CurrentView = new TagViewModel();
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
        //new ListItemTemplate(typeof(WorkViewModel), "Works", "fa-thin fa-calendar-days")
    };

    [ObservableProperty] private ListItemTemplate _selectedItem;

    partial void OnSelectedItemChanged(ListItemTemplate? value)
    {
        if (value is null) return;
        var instance = Activator.CreateInstance(value.ViewModelType);
        if (instance is null) return;
        CurrentView = (ViewModelBase)instance;
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