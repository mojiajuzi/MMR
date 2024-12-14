using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Controls.Notifications;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using MMR.Data;
using MMR.Models;

namespace MMR.ViewModels;

public partial class ContactViewModel : ViewModelBase
{
    [ObservableProperty] private bool _hasErrors;
    [ObservableProperty] private string _errorMessage;

    [ObservableProperty] private bool _isPopupOpen;
    [ObservableProperty] private Contact _contactData;

    [ObservableProperty] private ObservableCollection<Tag> _tagList;
    [ObservableProperty] private ObservableCollection<Tag> _filterTagList = [];
    [ObservableProperty] private string _tagSearchText;
    [ObservableProperty] private ObservableCollection<Tag> _selectedTagList = [];
    [ObservableProperty] private Tag _selectedTag;

    [ObservableProperty] private Bitmap _avatar;

    [ObservableProperty] private Collection<Contact> _contacts;

    public ContactViewModel()
    {
        GetContacts();
    }

    private void GetContacts()
    {
        try
        {
            var contacts = DbHelper.Db.Contacts
                .Include(c => c.ContactTags)
                .ThenInclude(ct => ct.Tag)
                .OrderByDescending(c => c.UpdatedAt)
                .AsNoTracking()
                .ToList();

            Contacts = new ObservableCollection<Contact>(contacts);
        }
        catch (Exception ex)
        {
            ShowNotification("Error", ex.Message, NotificationType.Error);
        }
    }

    [RelayCommand]
    private void OpenPopup()
    {
        ContactData = new Contact();
        var list = GetTagList();
        TagList = new ObservableCollection<Tag>(list);
        FilterTagList = new ObservableCollection<Tag>(list);
        SelectedTagList = new ObservableCollection<Tag>();
        IsPopupOpen = true;
    }

    private List<Tag> GetTagList()
    {
        return DbHelper.Db.Tags.AsNoTracking().ToList();
    }

    partial void OnTagSearchTextChanged(string value)
    {
        if (string.IsNullOrEmpty(TagSearchText))
        {
            FilterTagList = new ObservableCollection<Tag>(
                TagList.Where(t => !SelectedTagList.Contains(t))
            );
            return;
        }

        var filter = TagList
            .Where(t => t.Name.Contains(TagSearchText, StringComparison.OrdinalIgnoreCase))
            .Where(t => !SelectedTagList.Contains(t));

        FilterTagList = new ObservableCollection<Tag>(filter);
    }

    [RelayCommand]
    private void SelectTag(Tag tag)
    {
        if (tag != null && !SelectedTagList.Contains(tag))
        {
            SelectedTagList.Add(tag);
            FilterTagList.Remove(tag);
            TagSearchText = string.Empty;
        }
    }

    [RelayCommand]
    private void RemoveTag(Tag tag)
    {
        if (tag != null)
        {
            SelectedTagList.Remove(tag);
            FilterTagList = new ObservableCollection<Tag>(
                TagList.Where(t => !SelectedTagList.Contains(t))
            );
        }
    }


    public void SetContactAvatar(Bitmap bitmap, string? destinationPath)
    {
        if (bitmap != null && destinationPath != null)
        {
            Avatar = bitmap;
            ContactData.Avatar = destinationPath;
        }
    }

    [RelayCommand]
    private void PopupClose()
    {
        ContactData = null;
        IsPopupOpen = false;
    }

    [RelayCommand]
    private void ContactSubmited()
    {
        try
        {
            // 基本验证
            if (string.IsNullOrWhiteSpace(ContactData.Name))
            {
                HasErrors = true;
                ErrorMessage = "联系人姓名不能为空";
                return;
            }

            using var transaction = DbHelper.Db.Database.BeginTransaction();
            try
            {
                if (ContactData.Id > 0)
                {
                    // 更新联系人
                    var contact = DbHelper.Db.Contacts
                        .Include(c => c.ContactTags)
                        .First(c => c.Id == ContactData.Id);

                    // 更新基本信息
                    contact.Name = ContactData.Name;
                    contact.Email = ContactData.Email?.Trim();
                    contact.Phone = ContactData.Phone?.Trim();
                    contact.Wechat = ContactData.Wechat?.Trim();
                    contact.QQ = ContactData.QQ?.Trim();
                    contact.Remark = ContactData.Remark?.Trim();
                    contact.Avatar = ContactData.Avatar;
                    contact.UpdatedAt = DateTime.Now;

                    // 更新标签关联
                    // 先删除所有现有标签
                    DbHelper.Db.ContactTags.RemoveRange(contact.ContactTags);
                    DbHelper.Db.SaveChanges();

                    // 添加新的标签关联
                    if (SelectedTagList.Any())
                    {
                        var contactTags = SelectedTagList.Select(tag => new ContactTag
                        {
                            ContactId = contact.Id,
                            TagId = tag.Id,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        }).ToList();

                        DbHelper.Db.ContactTags.AddRange(contactTags);
                    }
                }
                else
                {
                    // 新增联系人
                    var contact = new Contact
                    {
                        Name = ContactData.Name,
                        Email = ContactData.Email?.Trim(),
                        Phone = ContactData.Phone?.Trim(),
                        Wechat = ContactData.Wechat?.Trim(),
                        QQ = ContactData.QQ?.Trim(),
                        Remark = ContactData.Remark?.Trim(),
                        Avatar = ContactData.Avatar,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    DbHelper.Db.Contacts.Add(contact);
                    DbHelper.Db.SaveChanges();

                    // 添加标签关联
                    if (SelectedTagList.Any())
                    {
                        var contactTags = SelectedTagList.Select(tag => new ContactTag
                        {
                            ContactId = contact.Id,
                            TagId = tag.Id,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        }).ToList();

                        DbHelper.Db.ContactTags.AddRange(contactTags);
                    }
                }

                DbHelper.Db.SaveChanges();
                transaction.Commit();

                // 重置状态
                HasErrors = false;
                ErrorMessage = string.Empty;
                IsPopupOpen = false;

                ShowNotification("Success", "data commit successed", NotificationType.Success);
                // 刷新联系人列表
                GetContacts();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                HasErrors = true;
                ErrorMessage = ex.Message;
                return;
                throw new Exception($"保存过程中出错: {ex.Message}\n{ex.InnerException?.Message ?? ""}", ex);
            }
        }
        catch (Exception ex)
        {
            HasErrors = true;
            ErrorMessage = $"保存失败: {ex.Message}";
            return;
            System.Diagnostics.Debug.WriteLine($"Contact save error: {ex}");
        }
    }

    [RelayCommand]
    private void ShowPopupToUpdate(Contact contact)
    {
        try
        {
            // 设置联系人数据
            ContactData = contact;

            // 加载头像
            if (!string.IsNullOrEmpty(ContactData.Avatar) && File.Exists(ContactData.Avatar))
            {
                Avatar = new Bitmap(ContactData.Avatar);
            }
            else
            {
                Avatar = null;
            }

            TagList = new ObservableCollection<Tag>(GetTagList());

            // 设置已选择的标签
            var selectedTags = contact.ContactTags
                .Select(ct => ct.Tag)
                .ToList();

            // 设置可选择的标签（排除已选择的）
            var availableTags = TagList
                .Where(t => !selectedTags.Select(st => st.Id).Contains(t.Id))
                .ToList();

            // 更新标签列表
            SelectedTagList = new ObservableCollection<Tag>(selectedTags);
            FilterTagList = new ObservableCollection<Tag>(availableTags);

            IsPopupOpen = true;
        }
        catch (Exception ex)
        {
            ShowNotification("Error", $"加载联系人信息失败: {ex.Message}", NotificationType.Error);
        }
    }

    [RelayCommand]
    private void RemoveContact(Contact contact)
    {
    }

    private void ShowNotification(string title, string message, NotificationType type)
    {
        var notification = new Notification(title, message, type);
        App.NotificationManager?.Show(notification);
    }
}