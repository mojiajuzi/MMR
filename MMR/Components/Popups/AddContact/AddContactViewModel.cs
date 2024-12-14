using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using MMR.Data;
using MMR.Models;
using MMR.ViewModels;

namespace MMR.Components.Popups.AddContact;

public partial class AddContactViewModel : ViewModelBase
{
    // 搜索相关
    [ObservableProperty] private string _searchText;
    [ObservableProperty] private bool _isSearching;
    [ObservableProperty] private ObservableCollection<Contact> _filteredContacts;

    // 选中的联系人
    [ObservableProperty] private WorkContact _workContactData;
    [ObservableProperty] private Contact _selectedContact;

    // 错误处理
    [ObservableProperty] private bool _hasErrors;
    [ObservableProperty] private string _errorMessage;

    // 当前工作项目
    private Work _currentWork;

    // 定义联系人添加成功事件
    public event EventHandler<WorkContact> ContactAdded;

    public AddContactViewModel()
    {
        FilteredContacts = new ObservableCollection<Contact>();
    }

    public void Open(Work work)
    {
        _currentWork = work;
        Reset();
    }

    private void Reset()
    {
        SearchText = string.Empty;
        SelectedContact = null;
        WorkContactData = new WorkContact();
        FilteredContacts.Clear();
        HasErrors = false;
        ErrorMessage = string.Empty;
    }

    // 当搜索文本改变时触发
    partial void OnSearchTextChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            IsSearching = false;
            FilteredContacts.Clear();
            return;
        }

        IsSearching = true;
        SearchContacts(value);
    }

    private void SearchContacts(string searchText)
    {
        try
        {
            var contacts = DbHelper.Db.Contacts
                .AsNoTracking()
                .Where(c => c.Name.Contains(searchText) ||
                            (c.Email != null && c.Email.Contains(searchText)))
                .Take(10)
                .ToList();

            FilteredContacts.Clear();
            foreach (var contact in contacts)
            {
                FilteredContacts.Add(contact);
            }
        }
        catch (Exception ex)
        {
            HasErrors = true;
            ErrorMessage = $"搜索联系人失败：{ex.Message}";
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        Reset();
        ContactAdded?.Invoke(this, null); // 通知取消
    }

    [RelayCommand]
    private async Task Submit()
    {
        try
        {
            HasErrors = false;
            ErrorMessage = string.Empty;

            // 验证
            if (SelectedContact == null)
            {
                HasErrors = true;
                ErrorMessage = "请选择联系人";
                return;
            }

            if (_currentWork == null)
            {
                HasErrors = true;
                ErrorMessage = "未找到当前工作项目";
                return;
            }

            // 检查是否已存在
            var exists = await DbHelper.Db.WorkContacts
                .AnyAsync(wc => wc.WorkId == _currentWork.Id &&
                                wc.ContactId == SelectedContact.Id);

            if (exists)
            {
                HasErrors = true;
                ErrorMessage = "该联系人已添加到当前工作";
                return;
            }

            // 创建新的关联
            var workContact = new WorkContact
            {
                WorkId = _currentWork.Id,
                ContactId = SelectedContact.Id,
                Money = WorkContactData.Money,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await DbHelper.Db.WorkContacts.AddAsync(workContact);
            await DbHelper.Db.SaveChangesAsync();

            ShowNotification("Success", "联系人添加成功", NotificationType.Success);

            // 触发事件
            ContactAdded?.Invoke(this, workContact);
            Reset();
        }
        catch (Exception ex)
        {
            HasErrors = true;
            ErrorMessage = $"添加联系人失败：{ex.Message}";
        }
    }
}