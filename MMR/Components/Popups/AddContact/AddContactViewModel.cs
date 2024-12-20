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
    public event EventHandler<WorkContactEventArgs> ContactAdded;

    private bool _isEdit;
    private int _editWorkContactId;

    public AddContactViewModel()
    {
        FilteredContacts = new ObservableCollection<Contact>();
    }

    public void Open(Work work)
    {
        _currentWork = work;
        _isEdit = false;
        _editWorkContactId = 0;
        Reset();
    }

    public void OpenForEdit(Work work, WorkContact workContact)
    {
        _currentWork = work;
        _isEdit = true;
        _editWorkContactId = workContact.Id;

        // 设置编辑数据
        WorkContactData = new WorkContact
        {
            Money = workContact.Money
        };
        SelectedContact = workContact.Contact;
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
        ContactAdded?.Invoke(this, new WorkContactEventArgs(null, _isEdit)); // 通知取消
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

            if (_isEdit)
            {
                // 更新现有记录
                var workContact = await DbHelper.Db.WorkContacts
                    .FirstOrDefaultAsync(wc => wc.Id == _editWorkContactId);

                if (workContact == null)
                {
                    HasErrors = true;
                    ErrorMessage = "未找到要编辑的记录";
                    return;
                }

                workContact.Money = WorkContactData.Money;
                workContact.UpdatedAt = DateTime.UtcNow;
                DbHelper.Db.WorkContacts.Update(workContact);
            }
            else
            {
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

                // 创建新记录
                var workContact = new WorkContact
                {
                    WorkId = _currentWork.Id,
                    ContactId = SelectedContact.Id,
                    Money = WorkContactData.Money,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await DbHelper.Db.WorkContacts.AddAsync(workContact);
            }

            await DbHelper.Db.SaveChangesAsync();

            ShowNotification("Success", _isEdit ? "联系人更新成功" : "联系人添加成功", NotificationType.Success);

            // 触发事件
            var updatedWorkContact = await DbHelper.Db.WorkContacts
                .Include(wc => wc.Contact)
                .FirstAsync(wc => wc.WorkId == _currentWork.Id && wc.ContactId == SelectedContact.Id);
            ContactAdded?.Invoke(this, new WorkContactEventArgs(updatedWorkContact, _isEdit));

            Reset();
        }
        catch (Exception ex)
        {
            HasErrors = true;
            ErrorMessage = $"操作失败：{ex.Message}";
        }
    }

    [RelayCommand]
    private void ClearContact()
    {
        SelectedContact = null;
        SearchText = string.Empty;
        IsSearching = false;
        FilteredContacts.Clear();
    }

    partial void OnSelectedContactChanged(Contact value)
    {
        if (value != null)
        {
            // 选中联系人后，清空搜索状态
            SearchText = string.Empty;
            IsSearching = false;
            FilteredContacts.Clear();
        }
    }
}

public class WorkContactEventArgs : EventArgs
{
    public WorkContact WorkContact { get; set; }
    public bool IsEdit { get; set; }

    public WorkContactEventArgs(WorkContact workContact, bool isEdit)
    {
        WorkContact = workContact;
        IsEdit = isEdit;
    }
}