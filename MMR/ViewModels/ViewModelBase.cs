using System;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MMR.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    protected void ShowNotification(string title, string message, NotificationType type)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var notification = new Notification()
            {
                Title = title,
                Message = message,
                Type = type
            };

            App.NotificationManager?.Show(notification);
        });
    }
}