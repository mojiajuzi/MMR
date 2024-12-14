using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using MMR;
using MMR.Components.Dialogs;

namespace MMR.Services;

public class DialogService : IDialogService
{
    private Window GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }

        throw new InvalidOperationException("Cannot find MainWindow");
    }

    public async Task<bool> ShowConfirmAsync(string title, string message, string okText = "确定",
        string cancelText = "取消")
    {
        var dialog = new ConfirmDialog
        {
            Title = title,
            Message = message,
            OkText = okText,
            CancelText = cancelText
        };

        var tcs = new TaskCompletionSource<bool>();

        void OnOk(object sender, RoutedEventArgs e)
        {
            tcs.SetResult(true);
            dialog.Close();
        }

        void OnCancel(object sender, RoutedEventArgs e)
        {
            tcs.SetResult(false);
            dialog.Close();
        }

        dialog.OkClicked += OnOk;
        dialog.CancelClicked += OnCancel;

        await dialog.ShowDialog(GetMainWindow());
        return await tcs.Task;
    }
}