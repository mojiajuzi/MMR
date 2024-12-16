using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using MMR.Data;
using MMR.Models;
using MMR.Services;

namespace MMR.ViewModels;

public partial class TagViewModel : ViewModelBase
{
    [ObservableProperty] private bool _hasErrors;
    [ObservableProperty] private string _errorMessage = string.Empty;

    [ObservableProperty] private bool _isPopupOpen;
    [ObservableProperty] private Tag _tagData;

    [ObservableProperty] private ObservableCollection<Tag> _tags;

    [ObservableProperty] 
    private string _searchText;

    partial void OnSearchTextChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            // 如果搜索文本为空，显示所有标签
            Tags = new ObservableCollection<Tag>(GetTags());
            return;
        }

        // 搜索标签
        var filtered = DbHelper.Db.Tags
            .AsNoTracking()
            .Where(t => t.Name.Contains(value))
            .OrderByDescending(t => t.UpdatedAt)
            .ToList();

        Tags = new ObservableCollection<Tag>(filtered);
    }

    private List<Tag> GetTags()
    {
        return DbHelper.Db.Tags
            .AsNoTracking()
            .OrderByDescending(t => t.UpdatedAt)
            .ToList();
    }

    public TagViewModel()
    {
        Tags = new ObservableCollection<Tag>(GetTags());
    }

    private void RefreshTags()
    {
        // 如果有搜索文本，保持搜索状态
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            OnSearchTextChanged(SearchText);
        }
        else
        {
            Tags = new ObservableCollection<Tag>(GetTags());
        }
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

    private bool ValidateTag()
    {
        ClearErrors();

        // 使用 DataAnnotations 进行验证
        var validationContext = new ValidationContext(TagData);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(TagData, validationContext, validationResults, true);

        if (!isValid)
        {
            HasErrors = true;
            ErrorMessage = validationResults.First().ErrorMessage ?? string.Empty;
            return false;
        }

        // 检查名称是否已存在（这个验证不能通过 DataAnnotations 实现）
        if (_tags.Any(t => t.Name.Equals(TagData.Name, StringComparison.OrdinalIgnoreCase) 
            && t.Id != TagData.Id))
        {
            AddError(nameof(TagData.Name), Lang.Resources.TagNameExists);
            return false;
        }

        return true;
    }

    [RelayCommand]
    private async Task TagSubmited()
    {
        if (TagData == null) return;
        
        if (!ValidateTag())
        {
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
                    AddError(nameof(TagData.Name), Lang.Resources.TagNameExists);
                    return;
                }

                DbHelper.Db.Tags.Update(TagData);
                await DbHelper.Db.SaveChangesAsync();
                var index = Tags.IndexOf(Tags.FirstOrDefault(t => t.Id == TagData.Id));
                Tags.RemoveAt(index);
                Tags.Insert(index, TagData);
                var msg = LangCombService.Succerss(Lang.Resources.Tag, TagData.Name, true);
                ShowNotification(Lang.Resources.Success, msg, NotificationType.Success);
                IsPopupOpen = false;
            }
            else
            {
                DbHelper.Db.Tags.Add(TagData);
                Tags.Add(TagData);
                var msg = LangCombService.Succerss(Lang.Resources.Tag, TagData.Name, false);
                ShowNotification(Lang.Resources.Success, msg, NotificationType.Success);
                TagData = new Tag();
                await DbHelper.Db.SaveChangesAsync();
            }
        }
        catch (Exception e)
        {
            AddError(string.Empty, e.Message);
            return;
        }
        RefreshTags();
    }

    [RelayCommand]
    private async Task ActiveChange(Tag tag)
    {
        var t = DbHelper.Db.Tags.FirstOrDefault(t => t.Id == tag.Id);
        if (t == null) return;
        try
        {
            t.IsActive = tag.IsActive;
            await DbHelper.Db.SaveChangesAsync();
            var status = tag.IsActive ? Lang.Resources.Active : Lang.Resources.InActive;
            var msg = $"{Lang.Resources.Tag} {tag.Name} {Lang.Resources.Status}: {status}";
            ShowNotification(Lang.Resources.Success, msg, NotificationType.Success);
        }
        catch (Exception e)
        {
            ShowNotification(Lang.Resources.Error, e.Message, NotificationType.Error);
        }
        RefreshTags();
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
            var msg = LangCombService.Succerss(Lang.Resources.Tag, tag.Name, true);
            ShowNotification(Lang.Resources.Success, msg, NotificationType.Success);
        }
        catch (Exception e)
        {
            ShowNotification(Lang.Resources.Error, e.Message, NotificationType.Error);
        }
    }

    private void AddError(string propertyName, string errorMessage)
    {
        HasErrors = true;
        ErrorMessage = errorMessage;
    }

    private void ClearErrors()
    {
        HasErrors = false;
        ErrorMessage = string.Empty;
    }
}