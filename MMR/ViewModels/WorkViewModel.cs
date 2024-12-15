using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using MMR.Components.Popups.AddContact;
using MMR.Components.Popups.AddExpense;
using MMR.Data;
using MMR.Models;
using MMR.Models.Enums;
using MMR.Services;

namespace MMR.ViewModels;

public partial class WorkViewModel : ViewModelBase
{
    [ObservableProperty] private bool _hasErrors;
    [ObservableProperty] private string _errorMessage;
    [ObservableProperty] private bool _isWorkPopupOpen;
    [ObservableProperty] private bool _isDetailsPaneOpen;
    [ObservableProperty] private Work _workData;

    [ObservableProperty] private DateTimeOffset? _selectedStartAt;
    [ObservableProperty] private DateTimeOffset? _selectedEndAt;

    [ObservableProperty] private ObservableCollection<Work> _works;

    [ObservableProperty] private ObservableCollection<WorkStatus> _statusList;
    [ObservableProperty] private string _searchText;

    [ObservableProperty] private Work _workDetails;

    //contact add 
    [ObservableProperty] private bool _contactPopupOpen;

    [ObservableProperty] private bool _isContactPopupOpen;
    [ObservableProperty] private bool _isExpensePopupOpen;

    private readonly AddContactViewModel _addContactViewModel;
    private readonly AddExpenseViewModel _addExpenseViewModel;

    private readonly IDialogService _dialogService;

    public AddContactViewModel AddContactViewModel => _addContactViewModel;
    public AddExpenseViewModel AddExpenseViewModel => _addExpenseViewModel;

    [ObservableProperty] private decimal _receivingPayment;
    [ObservableProperty] private decimal _cost;

    public WorkViewModel(AddContactViewModel addContactViewModel, AddExpenseViewModel addExpenseViewModel,
        IDialogService dialogService)
    {
        _addContactViewModel = addContactViewModel;
        _addExpenseViewModel = addExpenseViewModel;
        _dialogService = dialogService;

        Works = new ObservableCollection<Work>(GetWorks());
        StatusList = new ObservableCollection<WorkStatus>(Enum.GetValues<WorkStatus>());

        // 订阅事件
        _addContactViewModel.ContactAdded += OnContactAdded;
        _addExpenseViewModel.ExpenseAdded += OnExpenseAdded;
    }

    private List<Work> GetWorks()
    {
        var list = DbHelper.Db.Works.Include(w => w.Expenses).AsNoTracking().ToList();
        return list;
    }

    partial void OnSearchTextChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Works = new ObservableCollection<Work>(GetWorks());
            return;
        }

        var filtered = DbHelper.Db.Works
            .AsNoTracking()
            .Where(w => w.Name.Contains(value) ||
                        w.Description.Contains(value))
            .ToList();

        Works = new ObservableCollection<Work>(filtered);
    }

    [RelayCommand]
    private void OpenWorkPopup()
    {
        WorkData = new Work
        {
            Status = WorkStatus.PreStart // 设置默认状态
        };
        IsWorkPopupOpen = true;
    }

    [RelayCommand]
    private void CloseWorkPopup()
    {
        WorkData = new Work();
        IsWorkPopupOpen = false;
    }

    [RelayCommand]
    private async Task SubmitWork()
    {
        try
        {
            HasErrors = false;
            ErrorMessage = string.Empty;

            // 数据验证
            if (string.IsNullOrWhiteSpace(WorkData.Name))
            {
                HasErrors = true;
                ErrorMessage = "名称不能为空";
                return;
            }

            if (SelectedStartAt == null)
            {
                HasErrors = true;
                ErrorMessage = "开始时间不能为空";
                return;
            }

            // 设置时间
            WorkData.StartAt = SelectedStartAt.Value.DateTime;
            WorkData.EndAt = SelectedEndAt?.DateTime;

            // 设置创建和更新时间
            if (WorkData.Id == 0) // 新建
            {
                WorkData.CreatedAt = DateTime.UtcNow;
                WorkData.UpdatedAt = DateTime.UtcNow;
                await DbHelper.Db.Works.AddAsync(WorkData);
            }
            else // 更新
            {
                WorkData.UpdatedAt = DateTime.UtcNow;
                DbHelper.Db.Works.Update(WorkData);
            }

            await DbHelper.Db.SaveChangesAsync();

            ShowNotification("success", "Success", NotificationType.Success);
            // 关闭弹窗并重置数据
            IsWorkPopupOpen = false;
            WorkData = new Work();
            SelectedStartAt = null;
            SelectedEndAt = null;

            //更新works
            Works = new ObservableCollection<Work>(GetWorks());
        }
        catch (Exception ex)
        {
            HasErrors = true;
            ErrorMessage = $"保存失败：{ex.Message}";
        }
    }

    [RelayCommand]
    public void ShowPopupToWorkUpdate(Work work)
    {
        WorkData = work;
        SelectedStartAt = work.StartAt;
        SelectedEndAt = work.EndAt;
        IsWorkPopupOpen = true;
    }

    [RelayCommand]
    private async Task DeleteWork(Work work)
    {
        var result = await _dialogService.ShowConfirmAsync(
            "删除确认",
            "确定要删除这个工作项目吗？这将同时删除所有相关的联系人和支出记录。",
            "删除",
            "取消"
        );

        if (!result) return;

        try
        {
            // 先从数据库获取要删除的实体
            var workToDelete = await DbHelper.Db.Works
                .Include(w => w.WorkContacts)
                .Include(w => w.Expenses)
                .FirstOrDefaultAsync(w => w.Id == work.Id);

            if (workToDelete != null)
            {
                // 删除关联的联系人和支出记录
                DbHelper.Db.WorkContacts.RemoveRange(workToDelete.WorkContacts);
                DbHelper.Db.Expenses.RemoveRange(workToDelete.Expenses);
                DbHelper.Db.Works.Remove(workToDelete);

                await DbHelper.Db.SaveChangesAsync();

                // 如果当前正在查看这个工作的详情，则关闭详情面板
                if (WorkDetails?.Id == work.Id)
                {
                    IsDetailsPaneOpen = false;
                    WorkDetails = null;
                }

                // 刷新工作列表
                Works = new ObservableCollection<Work>(GetWorks());

                ShowNotification("Success", "工作项目删除成功", NotificationType.Success);
            }
        }
        catch (Exception ex)
        {
            ShowNotification("Error", $"删除失败：{ex.Message}", NotificationType.Error);
        }
    }

    [RelayCommand]
    private void OpenDetailsPane(Work work)
    {
        // 如果已经打开且是同一个work，则关闭
        if (IsDetailsPaneOpen && WorkDetails?.Id == work.Id)
        {
            IsDetailsPaneOpen = false;
            WorkDetails = null;
            return;
        }

        // 获取详细数据
        var detail = DbHelper.Db.Works.AsNoTracking()
            .Include(w => w.Expenses)
            .ThenInclude(c => c.Contact)
            .Include(w => w.WorkContacts)
            .ThenInclude(wc => wc.Contact)
            .FirstOrDefault(w => w.Id == work.Id);

        if (detail == null) return;

        // 更新数据和状态
        WorkDetails = detail;
        IsDetailsPaneOpen = true;

        UpdateWorkCalculations();
    }

    [RelayCommand]
    private void CloseDetailsPane()
    {
        IsDetailsPaneOpen = false;
        WorkDetails = null;
    }

    [RelayCommand]
    private void AddContact()
    {
        IsExpensePopupOpen = false;
        if (WorkDetails == null)
        {
            return;
        }

        _addContactViewModel.Open(WorkDetails);
        IsContactPopupOpen = true;
    }

    private void OnContactAdded(object sender, WorkContactEventArgs e)
    {
        if (e.WorkContact == null)
        {
            IsContactPopupOpen = false;
            return; // 用户取消
        }

        if (e.IsEdit)
        {
            IsContactPopupOpen = false;
        }

        // 刷新当前工作详情
        var detail = DbHelper.Db.Works.AsNoTracking()
            .Include(w => w.Expenses)
            .ThenInclude(e => e.Contact)
            .Include(w => w.WorkContacts)
            .ThenInclude(wc => wc.Contact)
            .FirstOrDefault(w => w.Id == WorkDetails.Id);

        if (detail != null)
        {
            WorkDetails = detail;
        }

        UpdateWorkCalculations();
    }

    private void OnExpenseAdded(object? sender, ExpenseEventArgs e)
    {
        if (e.Expense == null)
        {
            IsExpensePopupOpen = false;
            return; // 用户取消
        }

        if (e.IsEdit)
        {
            IsExpensePopupOpen = false;
        }

        // 刷新当前工作详情
        var detail = DbHelper.Db.Works.AsNoTracking()
            .Include(w => w.Expenses)
            .ThenInclude(e => e.Contact)
            .Include(w => w.WorkContacts)
            .ThenInclude(wc => wc.Contact)
            .FirstOrDefault(w => w.Id == WorkDetails.Id);

        if (detail != null)
        {
            WorkDetails = detail;
        }

        UpdateWorkCalculations();
    }

    [RelayCommand]
    private void AddExpense()
    {
        IsContactPopupOpen = false;
        if (WorkDetails == null) return;
        _addExpenseViewModel.Open(WorkDetails);
        IsExpensePopupOpen = true;
    }

    [RelayCommand]
    private void EditExpense(Expense expense)
    {
        if (WorkDetails == null) return;
        _addExpenseViewModel.OpenForEdit(WorkDetails, expense);
        IsExpensePopupOpen = true;
    }

    [RelayCommand]
    private async Task DeleteExpense(Expense expense)
    {
        if (WorkDetails == null) return;

        var result = await _dialogService.ShowConfirmAsync(
            "删除确认",
            "确定要删除这条支出记录吗？",
            "删除",
            "取消"
        );

        if (!result) return;

        // 先从数据库获取要删除的实体
        var expenseToDelete = await DbHelper.Db.Expenses
            .FirstOrDefaultAsync(e => e.Id == expense.Id);

        if (expenseToDelete != null)
        {
            DbHelper.Db.Expenses.Remove(expenseToDelete);
            await DbHelper.Db.SaveChangesAsync();

            // 刷新详情
            var detail = await DbHelper.Db.Works.AsNoTracking()
                .Include(w => w.Expenses)
                .ThenInclude(e => e.Contact)
                .Include(w => w.WorkContacts)
                .ThenInclude(wc => wc.Contact)
                .FirstOrDefaultAsync(w => w.Id == WorkDetails.Id);

            if (detail != null)
            {
                WorkDetails = detail;
            }

            UpdateWorkCalculations();
        }
    }

    [RelayCommand]
    private void EditWorkContact(WorkContact workContact)
    {
        if (WorkDetails == null) return;
        _addContactViewModel.OpenForEdit(WorkDetails, workContact);
        IsContactPopupOpen = true;
    }

    [RelayCommand]
    private async Task DeleteWorkContact(WorkContact workContact)
    {
        if (WorkDetails == null) return;

        var result = await _dialogService.ShowConfirmAsync(
            "删除确认",
            "确定要删除这个联系人吗？",
            "删除",
            "取消"
        );

        if (!result) return;

        // 先从数据库获取要删除的实体
        var contactToDelete = await DbHelper.Db.WorkContacts
            .FirstOrDefaultAsync(wc => wc.Id == workContact.Id);

        if (contactToDelete != null)
        {
            DbHelper.Db.WorkContacts.Remove(contactToDelete);
            await DbHelper.Db.SaveChangesAsync();

            // 刷新详情
            var detail = await DbHelper.Db.Works.AsNoTracking()
                .Include(w => w.Expenses)
                .ThenInclude(e => e.Contact)
                .Include(w => w.WorkContacts)
                .ThenInclude(wc => wc.Contact)
                .FirstOrDefaultAsync(w => w.Id == WorkDetails.Id);

            if (detail != null)
            {
                WorkDetails = detail;
            }

            UpdateWorkCalculations();
        }
    }

    private void UpdateWorkCalculations()
    {
        if (WorkDetails?.Expenses == null) return;

        ReceivingPayment = WorkDetails.Expenses.Where(e => e.Income).Sum(e => e.Amount);
        Cost = WorkDetails.Expenses.Where(e => !e.Income).Sum(e => e.Amount);
    }
}