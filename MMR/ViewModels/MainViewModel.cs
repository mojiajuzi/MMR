using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MMR.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private bool _panOpen;

    [RelayCommand]
    private void PanOpenTrigger()
    {
        PanOpen = !PanOpen;
    }
}