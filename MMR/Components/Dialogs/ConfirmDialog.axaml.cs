using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace MMR.Components.Dialogs;

public partial class ConfirmDialog : Window
{
    public event EventHandler<RoutedEventArgs> OkClicked;
    public event EventHandler<RoutedEventArgs> CancelClicked;

    public string Title
    {
        set => this.FindControl<TextBlock>("TitleBlock").Text = value;
    }

    public string Message
    {
        set => this.FindControl<TextBlock>("MessageBlock").Text = value;
    }

    public string OkText
    {
        set => this.FindControl<Button>("OkButton").Content = value;
    }

    public string CancelText
    {
        set => this.FindControl<Button>("CancelButton").Content = value;
    }

    public ConfirmDialog()
    {
        InitializeComponent();
        // 设置窗口属性
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        CanResize = false;
        SystemDecorations = SystemDecorations.None;

        // 绑定按钮事件
        this.FindControl<Button>("OkButton").Click += (s, e) => OkClicked?.Invoke(s, e);
        this.FindControl<Button>("CancelButton").Click += (s, e) => CancelClicked?.Invoke(s, e);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}