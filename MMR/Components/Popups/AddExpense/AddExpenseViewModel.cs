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

namespace MMR.Components.Popups.AddExpense;

public partial class AddExpenseViewModel : ViewModelBase
{
    // 搜索相关
    [ObservableProperty] private string _searchText;
    [ObservableProperty] private bool _isSearching;
    [ObservableProperty] private ObservableCollection<Contact> _filteredContacts;

    // 选中的联系人
    [ObservableProperty] private Contact _selectedContact;
    [ObservableProperty] private Expense _expenseData;

    // 错误处理
    [ObservableProperty] private bool _hasErrors;
    [ObservableProperty] private string _errorMessage;

    // 当前工作项目
    private Work _currentWork;

    // 定义支出添加成功事件
    public event EventHandler<ExpenseEventArgs> ExpenseAdded;

    //日期
    [ObservableProperty] private DateTimeOffset? _selectedDate;

    private bool _isEdit;
    private int _editExpenseId;

    public AddExpenseViewModel()
    {
        FilteredContacts = new ObservableCollection<Contact>();
        ExpenseData = new Expense { Date = DateTime.Today };
    }

    public void Open(Work work)
    {
        _currentWork = work;
        _isEdit = false;
        _editExpenseId = 0;
        Reset();
    }

    public void OpenForEdit(Work work, Expense expense)
    {
        _currentWork = work;
        _isEdit = true;
        _editExpenseId = expense.Id;

        // 设置编辑数据
        ExpenseData = new Expense
        {
            Amount = expense.Amount,
            Date = expense.Date,
            Income = expense.Income,
            Notes = expense.Notes
        };
        SelectedContact = expense.Contact;
        SelectedDate = expense.Date;
    }

    private void Reset()
    {
        SearchText = string.Empty;
        SelectedContact = null;
        ExpenseData = new Expense
        {
            Date = DateTime.Today,
            Income = false,
            Notes = string.Empty
        };
        SelectedDate = DateTime.Today;
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
            var contacts = DbHelper.Db.WorkContacts
                .AsNoTracking()
                .Where(wc => wc.WorkId == _currentWork.Id)
                .Include(wc => wc.Contact)
                .Where(wc => wc.Contact.Name.Contains(searchText) ||
                             (wc.Contact.Email != null && wc.Contact.Email.Contains(searchText)))
                .Select(wc => wc.Contact)
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
        ExpenseAdded?.Invoke(this, new ExpenseEventArgs(null, false));
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

            if (ExpenseData.Amount <= 0)
            {
                HasErrors = true;
                ErrorMessage = "金额必须大于0";
                return;
            }

            // 设置基本数据
            ExpenseData.WorkId = _currentWork.Id;
            ExpenseData.ContactId = SelectedContact.Id;
            ExpenseData.Date = SelectedDate?.Date ?? DateTime.Today;
            ExpenseData.UpdatedAt = DateTime.UtcNow;

            if (_isEdit)
            {
                // 更新现有记录
                ExpenseData.Id = _editExpenseId;
                DbHelper.Db.Expenses.Update(ExpenseData);
            }
            else
            {
                // 创建新记录
                ExpenseData.CreatedAt = DateTime.UtcNow;
                if (ExpenseData.Notes == null)
                {
                    ExpenseData.Notes = string.Empty;
                }

                await DbHelper.Db.Expenses.AddAsync(ExpenseData);
            }

            await DbHelper.Db.SaveChangesAsync();

            ShowNotification("Success", _isEdit ? "支出记录更新成功" : "支出记录添加成功", NotificationType.Success);

            // 触发事件
            ExpenseAdded?.Invoke(this, new ExpenseEventArgs(ExpenseData, _isEdit));
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

public class ExpenseEventArgs : EventArgs
{
    public Expense Expense { get; set; }
    public bool IsEdit { get; set; }

    public ExpenseEventArgs(Expense expense, bool isEdit)
    {
        Expense = expense;
        IsEdit = isEdit;
    }
}