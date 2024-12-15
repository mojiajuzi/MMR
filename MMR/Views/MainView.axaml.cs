using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Projektanker.Icons.Avalonia;

namespace MMR.Views;

public partial class MainView : UserControl
{
    private bool _isDarkTheme = false;

    public MainView()
    {
        InitializeComponent();
        InitializeTheme();
    }

    private void InitializeTheme()
    {
        var app = Application.Current;
        if (app == null) return;
        // 获取当前系统主题或保存的主题设置
        var currentTheme = app.ActualThemeVariant;
        _isDarkTheme = currentTheme == ThemeVariant.Dark;

        // 设置初始主题
        app.RequestedThemeVariant = _isDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;

        // 更新UI状态
        var toggleButton = this.FindControl<ToggleButton>("ThemeToggleButton");
        if (toggleButton != null)
        {
            toggleButton.IsChecked = _isDarkTheme;
        }

        UpdateThemeIcon();
    }

    private void ThemeToggleButton_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton toggleButton)
        {
            if (toggleButton.IsChecked == true)
            {
                IconToggle.Value = "fa-solid fa-moon";
                ThemeText.Text = "Dark Mode";
                Application.Current.RequestedThemeVariant = ThemeVariant.Dark;
            }
            else
            {
                IconToggle.Value = "fa-solid fa-sun";
                ThemeText.Text = "Light Mode";
                Application.Current.RequestedThemeVariant = ThemeVariant.Light;
            }
        }
    }

    private void SetTheme(bool isDark)
    {
        _isDarkTheme = isDark;
        var app = Application.Current;
        if (app != null)
        {
            app.RequestedThemeVariant = isDark ? ThemeVariant.Dark : ThemeVariant.Light;
        }

        UpdateThemeIcon();
    }

    private void UpdateThemeIcon()
    {
        var themeIcon = this.FindControl<Icon>("IconToggle");
        var ThemeText = this.FindControl<TextBlock>("ThemeText");
        if (themeIcon != null)
        {
            themeIcon.Value = _isDarkTheme ? "fa-solid fa-moon" : "fa-solid fa-sun";
            ThemeText.Text = _isDarkTheme ? "Dark" : "Light";
        }
    }
}