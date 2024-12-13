using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using MMR.Data;
using MMR.Models;

namespace MMR.ViewModels;

public partial class TagViewModel : ViewModelBase
{
    [ObservableProperty] private bool _hasErrors;
    [ObservableProperty] private string _errorMessage = string.Empty;

    [ObservableProperty] private bool _isPopupOpen;
    [ObservableProperty] private Tag _tagData;

    [ObservableProperty] private ObservableCollection<Tag> _tags;

    public TagViewModel()
    {
        GetTags();
    }

    private void GetTags()
    {
        var tagList = DbHelper.Db.Tags.ToList();
        Tags = new ObservableCollection<Tag>(tagList);
    }

    [RelayCommand]
    private void PopupOpen()
    {
        IsPopupOpen = true;
        TagData = new Tag();
    }

    [RelayCommand]
    private void PopupClose()
    {
        IsPopupOpen = false;
        TagData = null;
    }

    [RelayCommand]
    private async Task TagSubmited()
    {
        if (TagData == null) return;
        if (!TagData.Validate(out var result))
        {
            HasErrors = true;
            ErrorMessage = string.Join(Environment.NewLine, result.Select(r => r.ErrorMessage));
            return;
        }


        try
        {
            if (TagData.Id > 0)
            {
                var exiting = DbHelper.Db.Tags.AsNoTracking()
                    .FirstOrDefault(t => t.Name.ToLower() == TagData.Name.ToLower() && t.Id != TagData.Id);
                if (exiting != null)
                {
                    HasErrors = true;
                    ErrorMessage = $"tag name {TagData.Name} already exists!";
                    return;
                }

                DbHelper.Db.Tags.Update(TagData);
                await DbHelper.Db.SaveChangesAsync();
                var index = Tags.IndexOf(Tags.FirstOrDefault(t => t.Id == TagData.Id));
                Tags.RemoveAt(index);
                Tags.Insert(index, TagData);
                ShowNotification("success", "tag update success", NotificationType.Success);
                IsPopupOpen = false;
            }
            else
            {
                DbHelper.Db.Tags.Add(TagData);
                Tags.Add(TagData);
                ShowNotification("Success", "Successfully added tag", NotificationType.Success);
                TagData = new Tag();
                await DbHelper.Db.SaveChangesAsync();
            }
        }
        catch (Exception e)
        {
            HasErrors = true;
            ErrorMessage = e.Message;
            return;
        }
    }

    [RelayCommand]
    private void ActiveChange(Tag tag)
    {
        var t = DbHelper.Db.Tags.FirstOrDefault(t => t.Id == tag.Id);
        if (t == null) return;
        try
        {
            t.IsActive = tag.IsActive;
            DbHelper.Db.SaveChanges();
        }
        catch (Exception e)
        {
            ShowNotification("Error", e.Message, NotificationType.Error);
        }
    }

    [RelayCommand]
    private void ShowPopupToUpdate(Tag tag)
    {
        var t = DbHelper.Db.Tags.FirstOrDefault(t => t.Id == tag.Id);
        if (t == null) return;
        TagData = t;
        IsPopupOpen = true;
    }

    [RelayCommand]
    private void RemoveTag(Tag tag)
    {
        var t = DbHelper.Db.Tags.FirstOrDefault(t => t.Id == tag.Id);
        if (t == null) return;
        try
        {
            DbHelper.Db.Tags.Remove(t);
            DbHelper.Db.SaveChanges();
            Tags.Remove(tag);
            ShowNotification("Success", "Successfully removed tag", NotificationType.Success);
        }
        catch (Exception e)
        {
            ShowNotification("Error", e.Message, NotificationType.Error);
        }
    }
}