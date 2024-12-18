using System;
using Avalonia.Controls;
using Avalonia.Platform;

namespace MMR.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var uri = new Uri("avares://MMR/Assets/logo.ico");
        this.Icon = new WindowIcon(AssetLoader.Open(uri));
    }
}