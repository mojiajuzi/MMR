using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using Avalonia.Controls.Notifications;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using MMR.Data;
using MMR.Models;
using MMR.Services;

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

    [ObservableProperty] private string _searchText;

    // 添加一个用于存储原始数据的集合
    private List<Contact> _allContacts = new();

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

            _allContacts = contacts; // 保存原始数据
            Contacts = new ObservableCollection<Contact>(contacts);
        }
        catch (Exception ex)
        {
            ShowNotification(Lang.Resources.Error, ex.Message, NotificationType.Error);
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

    private bool ValidateContact()
    {
        ClearErrors();

        // 使用 DataAnnotations 进行验证
        var validationContext = new ValidationContext(ContactData);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(ContactData, validationContext, validationResults, true);

        if (!isValid)
        {
            HasErrors = true;
            ErrorMessage = validationResults.First().ErrorMessage ?? string.Empty;
            return false;
        }

        return true;
    }

    [RelayCommand]
    private void ContactSubmited()
    {
        try
        {
            if (!ValidateContact())
            {
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
                    DbHelper.Db.ContactTags.RemoveRange(contact.ContactTags);
                    DbHelper.Db.SaveChanges();

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

                HasErrors = false;
                ErrorMessage = string.Empty;
                IsPopupOpen = false;

                ShowNotification(Lang.Resources.Success, 
                               LangCombService.Succerss(Lang.Resources.Contact, ContactData.Name, ContactData.Id > 0), 
                               NotificationType.Success);
                GetContacts();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                AddError(string.Empty, ex.Message);
            }
        }
        catch (Exception ex)
        {
            AddError(string.Empty, ex.Message);
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
            ShowNotification(Lang.Resources.Error, ex.Message, NotificationType.Error);
        }
    }

    [RelayCommand]
    private void RemoveContact(Contact contact)
    {
        try
        {
            var c = DbHelper.Db.Contacts.FirstOrDefault(c => c.Id == contact.Id);
            if (c == null) return;
            
            DbHelper.Db.Contacts.Remove(c);
            DbHelper.Db.SaveChanges();
            Contacts.Remove(contact);
            
            var msg = LangCombService.Succerss(Lang.Resources.Contact, contact.Name, true);
            ShowNotification(Lang.Resources.Success, msg, NotificationType.Success);
        }
        catch (Exception ex)
        {
            ShowNotification(Lang.Resources.Error, ex.Message, NotificationType.Error);
        }
    }

    private void ShowNotification(string title, string message, NotificationType type)
    {
        var notification = new Notification(title, message, type);
        App.NotificationManager?.Show(notification);
    }

    // 监听SearchText的变化
    partial void OnSearchTextChanged(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            // 如果搜索文本为空，显示所有联系人
            Contacts = new ObservableCollection<Contact>(_allContacts);
            return;
        }

        // 执行搜索
        var searchResults = _allContacts.Where(c =>
            c.Name.Contains(value, StringComparison.OrdinalIgnoreCase) ||
            (c.Email?.Contains(value, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (c.Phone?.Contains(value, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (c.Wechat?.Contains(value, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (c.QQ?.Contains(value, StringComparison.OrdinalIgnoreCase) ?? false) ||
            c.ContactTags.Any(ct => 
                ct.Tag.Name.Contains(value, StringComparison.OrdinalIgnoreCase))
        ).ToList();

        Contacts = new ObservableCollection<Contact>(searchResults);
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private bool IsValidPhone(string phone)
    {
        // 简单的电话号码验证，可以根据需要修改正则表达式
        return System.Text.RegularExpressions.Regex.IsMatch(phone, @"^\d{11}$");
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