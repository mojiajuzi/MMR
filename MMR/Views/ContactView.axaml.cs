using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using MMR.Services;
using MMR.ViewModels;
using Projektanker.Icons.Avalonia;
using MMR.Models;

namespace MMR.Views;

public partial class ContactView : UserControl
{
    public ContactView()
    {
        InitializeComponent();
        DataContext = new ContactViewModel();
    }

    private async void AvatarUploadButtonClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            // 获取顶层窗口
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            // 打开文件选择器
            var pickedFiles = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "选择头像图片",
                AllowMultiple = false,
                FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
            });

            if (pickedFiles.Count == 0) return;

            var pickedFile = pickedFiles[0];

            // 生成唯一的文件名，避免文件名冲突
            string uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(pickedFile.Name)}";
            string uploadsPath = FilePathService.GetUserAvatarUploadsFolderPath();
            string? destinationPath = Path.Combine(uploadsPath, uniqueFileName);

            // 确保上传目录存在
            Directory.CreateDirectory(uploadsPath);

            // 读取并处理图片
            await using (var sourceStream = await pickedFile.OpenReadAsync())
            {
                var vm = DataContext as ContactViewModel;
                if (vm == null) return;

                // 将图片解码并调整大小
                var bitmap = await Task.Run(() => Bitmap.DecodeToWidth(sourceStream, 200));

                // 保存处理后的图片
                await using (var fileStream = File.Create(destinationPath))
                {
                    bitmap.Save(fileStream); // 直接保存处理后的图片
                }

                // 更新ViewModel
                vm.SetContactAvatar(bitmap, destinationPath);
            }
        }
        catch (Exception ex)
        {
            // 处理错误
            var note = new Notification("Error", ex.Message, NotificationType.Error);
            App.NotificationManager?.Show(note);
        }
    }

    private void TagSearchBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is ContactViewModel viewModel && e.AddedItems.Count > 0)
        {
            if (e.AddedItems[0] is Tag selectedTag)
            {
                viewModel.SelectTagCommand.Execute(selectedTag);
            }
            
            // 清除AutoCompleteBox的选择
            if (sender is AutoCompleteBox autoComplete)
            {
                autoComplete.SelectedItem = null;
            }
        }
    }
}