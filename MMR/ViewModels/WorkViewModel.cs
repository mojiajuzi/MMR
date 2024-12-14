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

    [ObservableProperty] private Work _workDetails;

    //contact add 
    [ObservableProperty] private bool _contactPopupOpen;

    [ObservableProperty] private bool _isContactPopupOpen;
    [ObservableProperty] private bool _isExpensePopupOpen;

    private readonly AddContactViewModel _addContactViewModel;
    private readonly AddExpenseViewModel _addExpenseViewModel;

    public AddContactViewModel AddContactViewModel => _addContactViewModel;
    public AddExpenseViewModel AddExpenseViewModel => _addExpenseViewModel;

    public WorkViewModel(AddContactViewModel addContactViewModel, AddExpenseViewModel addExpenseViewModel)
    {
        _addContactViewModel = addContactViewModel;
        _addExpenseViewModel = addExpenseViewModel;

        Works = new ObservableCollection<Work>(GetWorks());
        StatusList = new ObservableCollection<WorkStatus>(Enum.GetValues<WorkStatus>());

        // 订阅事件
        _addContactViewModel.ContactAdded += OnContactAdded;
        _addExpenseViewModel.ExpenseAdded += OnExpenseAdded;
    }

    private List<Work> GetWorks()
    {
        return DbHelper.Db.Works.AsNoTracking().ToList();
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

        // TODO: 添加确认对话框
        DbHelper.Db.Expenses.Remove(expense);
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

        // TODO: 添加确认对话框
        DbHelper.Db.WorkContacts.Remove(workContact);
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
    }
}