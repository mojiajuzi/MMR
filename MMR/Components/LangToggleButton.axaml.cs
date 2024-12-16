using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Controls.Primitives;
using MMR.Services;

namespace MMR.Components;

public partial class LangToggleButton : UserControl
{
    public LangToggleButton()
    {
        InitializeComponent();
        // 根据当前语言设置初始状态
        LangButton.IsChecked = LanguageService.Instance.IsCurrentLanguage("zh-Hans");
    }

    private void OnLangButtonCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton toggleButton)
        {
            // 根据切换状态改变语言
            LanguageService.Instance.ChangeLanguage(toggleButton.IsChecked == true ? "zh-Hans" : "en");
        }
    }
}