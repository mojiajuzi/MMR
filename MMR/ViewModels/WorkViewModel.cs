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
using System.ComponentModel.DataAnnotations;
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

    [ObservableProperty] private ObservableCollection<WorkStatus?> _statusList;
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

    [ObservableProperty] private WorkStatus? _selectedStatus;

    public WorkViewModel(AddContactViewModel addContactViewModel, AddExpenseViewModel addExpenseViewModel,
        IDialogService dialogService)
    {
        try
        {
            _addContactViewModel = addContactViewModel ?? throw new ArgumentNullException(nameof(addContactViewModel));
            _addExpenseViewModel = addExpenseViewModel ?? throw new ArgumentNullException(nameof(addExpenseViewModel));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            Works = new ObservableCollection<Work>(GetWorks());

            // 创建状态列表，添加 All 选项
            var statusList = new List<WorkStatus?> { null };  // null 代表 "All"
            statusList.AddRange(Enum.GetValues<WorkStatus>().Cast<WorkStatus?>());
            StatusList = new ObservableCollection<WorkStatus?>(statusList);

            // 订阅事件
            _addContactViewModel.ContactAdded += OnContactAdded;
            _addExpenseViewModel.ExpenseAdded += OnExpenseAdded;

            // 初始化其他属性
            ErrorMessage = string.Empty;
            HasErrors = false;
        }
        catch (Exception ex)
        {
            ShowNotification(Lang.Resources.Error, Lang.Resources.InitializationError, NotificationType.Error);
            System.Diagnostics.Debug.WriteLine($"WorkViewModel initialization error: {ex}");
        }
    }

    ~WorkViewModel()
    {
        try
        {
            if (_addContactViewModel != null)
            {
                _addContactViewModel.ContactAdded -= OnContactAdded;
            }
            if (_addExpenseViewModel != null)
            {
                _addExpenseViewModel.ExpenseAdded -= OnExpenseAdded;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WorkViewModel cleanup error: {ex}");
        }
    }

    private List<Work> GetWorks()
    {
        try
        {
            return DbHelper.Db.Works
                .Include(w => w.Expenses)
                .AsNoTracking()
                .OrderByDescending(w => w.UpdatedAt)
                .ToList();
        }
        catch (Exception ex)
        {
            ShowNotification(Lang.Resources.Error, Lang.Resources.LoadError, NotificationType.Error);
            return new List<Work>();
        }
    }

    partial void OnSelectedStatusChanged(WorkStatus? value)
    {
        try
        {
            IQueryable<Work> query = DbHelper.Db.Works.AsNoTracking();

            // 应用状态筛选
            if (value.HasValue)
            {
                query = query.Where(w => w.Status == value);
            }

            // 应用搜索条件（如果有）
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchTerm = SearchText.Trim().ToLower();
                query = query.Where(w => w.Name.ToLower().Contains(searchTerm) ||
                                       (w.Description != null && w.Description.ToLower().Contains(searchTerm)));
            }

            // 排序并获取结果
            var filtered = query.OrderByDescending(w => w.UpdatedAt).ToList();
            Works = new ObservableCollection<Work>(filtered);
        }
        catch (Exception ex)
        {
            ShowNotification(Lang.Resources.Error, Lang.Resources.LoadError, NotificationType.Error);
            System.Diagnostics.Debug.WriteLine($"Status filter error: {ex}");
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Works = new ObservableCollection<Work>(GetWorks());
            return;
        }

        try
        {
            var searchTerm = value.Trim().ToLower();
            var filtered = DbHelper.Db.Works
                .AsNoTracking()
                .Where(w => w.Name.ToLower().Contains(searchTerm) ||
                           (w.Description != null && w.Description.ToLower().Contains(searchTerm)) ||
                           w.TotalMoney.ToString().Contains(searchTerm) ||
                           w.Status.ToString().ToLower().Contains(searchTerm))
                .OrderByDescending(w => w.UpdatedAt)
                .ToList();

            Works = new ObservableCollection<Work>(filtered);
        }
        catch (Exception ex)
        {
            ShowNotification(Lang.Resources.Error, Lang.Resources.SearchError, NotificationType.Error);
            System.Diagnostics.Debug.WriteLine($"Search error: {ex}");
        }
    }

    [RelayCommand]
    private void OpenWorkPopup()
    {
        try
        {
            WorkData = new Work
            {
                Status = WorkStatus.PreStart,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            IsWorkPopupOpen = true;
        }
        catch (Exception ex)
        {
            ShowNotification(Lang.Resources.Error, ex.Message, NotificationType.Error);
        }
    }

    [RelayCommand]
    private void CloseWorkPopup()
    {
        try
        {
            WorkData = new Work();
            IsWorkPopupOpen = false;
            ClearErrors(); // 清除可能存在的错误信息
        }
        catch (Exception ex)
        {
            ShowNotification(Lang.Resources.Error, ex.Message, NotificationType.Error);
        }
    }

    private bool ValidateWork()
    {
        ClearErrors();

        // 使用 DataAnnotations 进行验证
        var validationContext = new ValidationContext(WorkData);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(WorkData, validationContext, validationResults, true);

        if (!isValid)
        {
            HasErrors = true;
            ErrorMessage = validationResults.First().ErrorMessage ?? string.Empty;
            return false;
        }

        // 验证必填日期（因为 DateTimeOffset 需要特殊处理）
        if (SelectedStartAt == null)
        {
            HasErrors = true;
            ErrorMessage = Lang.Resources.WorkStartTimeRequired;
            return false;
        }

        return true;
    }

    [RelayCommand]
    private async Task SubmitWork()
    {
        try
        {
            if (!ValidateWork())
            {
                return;
            }

            // 设置时间
            WorkData.StartAt = SelectedStartAt!.Value.DateTime;
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

            ShowNotification(Lang.Resources.Success, 
                           LangCombService.Succerss(Lang.Resources.Work, WorkData.Name, WorkData.Id > 0), 
                           NotificationType.Success);

            // 关闭弹窗并重置数据
            IsWorkPopupOpen = false;
            WorkData = new Work();
            SelectedStartAt = null;
            SelectedEndAt = null;

            // 更新works
            Works = new ObservableCollection<Work>(GetWorks());
        }
        catch (Exception ex)
        {
            AddError(string.Empty, ex.Message);
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

    [RelayCommand]
    public void ShowPopupToWorkUpdate(Work work)
    {
        if (work == null)
        {
            ShowNotification(Lang.Resources.Error, Lang.Resources.LoadError, NotificationType.Error);
            return;
        }

        WorkData = work;
        SelectedStartAt = work.StartAt;
        SelectedEndAt = work.EndAt;
        IsWorkPopupOpen = true;
    }

    [RelayCommand]
    private async Task DeleteWork(Work work)
    {
        var result = await _dialogService.ShowConfirmAsync(
            Lang.Resources.DeleteConfirmTitle,
            Lang.Resources.DeleteWorkConfirmMessage,
            Lang.Resources.Delete,
            Lang.Resources.Cancel
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

                var msg = LangCombService.Succerss(Lang.Resources.Work, work.Name, true);
                ShowNotification(Lang.Resources.Success, msg, NotificationType.Success);
            }
            else
            {
                ShowNotification(Lang.Resources.Error, Lang.Resources.LoadError, NotificationType.Error);
            }
        }
        catch (Exception ex)
        {
            ShowNotification(Lang.Resources.Error, ex.Message, NotificationType.Error);
        }
    }

    [RelayCommand]
    private void OpenDetailsPane(Work work)
    {
        try
        {
            // 如果已经打开且是同一个work，则关闭
            if (IsDetailsPaneOpen && WorkDetails?.Id == work.Id)
            {
                IsDetailsPaneOpen = false;
                WorkDetails = null;
                UpdateWorkCalculations(); // 清除计算结果
                return;
            }

            // 获取详细数据
            var detail = DbHelper.Db.Works.AsNoTracking()
                .Include(w => w.Expenses)
                .ThenInclude(e => e.Contact)
                .Include(w => w.WorkContacts)
                .ThenInclude(wc => wc.Contact)
                .FirstOrDefault(w => w.Id == work.Id);

            if (detail != null)
            {
                WorkDetails = detail;
                UpdateWorkCalculations();
                IsDetailsPaneOpen = true;
            }
            else
            {
                ShowNotification(Lang.Resources.Error, Lang.Resources.LoadError, NotificationType.Error);
            }
        }
        catch (Exception ex)
        {
            ShowNotification(Lang.Resources.Error, ex.Message, NotificationType.Error);
        }
    }

    [RelayCommand]
    private void CloseDetailsPane()
    {
        try
        {
            IsDetailsPaneOpen = false;
            WorkDetails = null;
            UpdateWorkCalculations(); // 清除计算结果
        }
        catch (Exception ex)
        {
            ShowNotification(Lang.Resources.Error, ex.Message, NotificationType.Error);
        }
    }

    [RelayCommand]
    private void AddContact()
    {
        try
        {
            IsExpensePopupOpen = false;
            if (WorkDetails == null)
            {
                ShowNotification(Lang.Resources.Error, Lang.Resources.LoadError, NotificationType.Error);
                return;
            }

            _addContactViewModel.Open(WorkDetails);
            IsContactPopupOpen = true;
        }
        catch (Exception ex)
        {
            ShowNotification(Lang.Resources.Error, ex.Message, NotificationType.Error);
        }
    }

    private void OnContactAdded(object sender, WorkContactEventArgs e)
    {
        if (e.WorkContact == null)
        {
            IsContactPopupOpen = false;
            return;
        }

        if (e.IsEdit)
        {
            IsContactPopupOpen = false;
            var msg = LangCombService.Succerss(Lang.Resources.Contact, e.WorkContact.Contact.Name, true);
            ShowNotification(Lang.Resources.Success, msg, NotificationType.Success);
        }
        else
        {
            var msg = LangCombService.Succerss(Lang.Resources.Contact, e.WorkContact.Contact.Name, false);
            ShowNotification(Lang.Resources.Success, msg, NotificationType.Success);
        }

        // 刷新当前工作详情
        RefreshWorkDetails();
    }

    private void OnExpenseAdded(object? sender, ExpenseEventArgs e)
    {
        if (e.Expense == null)
        {
            IsExpensePopupOpen = false;
            return;
        }

        if (e.IsEdit)
        {
            IsExpensePopupOpen = false;
            var msg = LangCombService.Succerss(Lang.Resources.Expense, e.Expense.Notes, true);
            ShowNotification(Lang.Resources.Success, msg, NotificationType.Success);
        }
        else
        {
            var msg = LangCombService.Succerss(Lang.Resources.Expense, e.Expense.Notes, false);
            ShowNotification(Lang.Resources.Success, msg, NotificationType.Success);
        }

        // 刷新当前工作详情
        RefreshWorkDetails();
    }

    // 提取重复的刷新逻辑为单独的方法
    private void RefreshWorkDetails()
    {
        try
        {
            if (WorkDetails == null) return;

            var detail = DbHelper.Db.Works.AsNoTracking()
                .Include(w => w.Expenses)
                .ThenInclude(e => e.Contact)
                .Include(w => w.WorkContacts)
                .ThenInclude(wc => wc.Contact)
                .FirstOrDefault(w => w.Id == WorkDetails.Id);

            if (detail != null)
            {
                WorkDetails = detail;
                UpdateWorkCalculations();
            }
            else
            {
                ShowNotification(Lang.Resources.Error, Lang.Resources.LoadError, NotificationType.Error);
            }
        }
        catch (Exception ex)
        {
            ShowNotification(Lang.Resources.Error, ex.Message, NotificationType.Error);
        }
    }

    [RelayCommand]
    private void AddExpense()
    {
        try
        {
            IsContactPopupOpen = false;
            if (WorkDetails == null)
            {
                ShowNotification(Lang.Resources.Error, Lang.Resources.LoadError, NotificationType.Error);
                return;
            }

            _addExpenseViewModel.Open(WorkDetails);
            IsExpensePopupOpen = true;
        }
        catch (Exception ex)
        {
            ShowNotification(Lang.Resources.Error, ex.Message, NotificationType.Error);
        }
    }

    [RelayCommand]
    private void EditExpense(Expense expense)
    {
        try
        {
            if (WorkDetails == null)
            {
                ShowNotification(Lang.Resources.Error, Lang.Resources.LoadError, NotificationType.Error);
                return;
            }

            _addExpenseViewModel.OpenForEdit(WorkDetails, expense);
            IsExpensePopupOpen = true;
        }
        catch (Exception ex)
        {
            ShowNotification(Lang.Resources.Error, ex.Message, NotificationType.Error);
        }
    }

    [RelayCommand]
    private async Task DeleteExpense(Expense expense)
    {
        if (WorkDetails == null) return;

        var result = await _dialogService.ShowConfirmAsync(
            Lang.Resources.DeleteConfirmTitle,
            Lang.Resources.DeleteExpenseConfirmMessage,
            Lang.Resources.Delete,
            Lang.Resources.Cancel
        );

        if (!result) return;

        try
        {
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
                    var msg = LangCombService.Succerss(Lang.Resources.Expense, expense.Notes, true);
                    ShowNotification(Lang.Resources.Success, msg, NotificationType.Success);
                }

                UpdateWorkCalculations();
            }
        }
        catch (Exception ex)
        {
            ShowNotification(Lang.Resources.Error, ex.Message, NotificationType.Error);
        }
    }

    [RelayCommand]
    private void EditWorkContact(WorkContact workContact)
    {
        try
        {
            if (WorkDetails == null)
            {
                ShowNotification(Lang.Resources.Error, Lang.Resources.LoadError, NotificationType.Error);
                return;
            }

            _addContactViewModel.OpenForEdit(WorkDetails, workContact);
            IsContactPopupOpen = true;
        }
        catch (Exception ex)
        {
            ShowNotification(Lang.Resources.Error, ex.Message, NotificationType.Error);
        }
    }

    [RelayCommand]
    private async Task DeleteWorkContact(WorkContact workContact)
    {
        if (WorkDetails == null) return;

        var result = await _dialogService.ShowConfirmAsync(
            Lang.Resources.DeleteConfirmTitle,
            Lang.Resources.DeleteWorkContactConfirmMessage,
            Lang.Resources.Delete,
            Lang.Resources.Cancel
        );

        if (!result) return;

        try
        {
            var contactToDelete = await DbHelper.Db.WorkContacts
                .FirstOrDefaultAsync(wc => wc.Id == workContact.Id);

            if (contactToDelete != null)
            {
                DbHelper.Db.WorkContacts.Remove(contactToDelete);
                await DbHelper.Db.SaveChangesAsync();

                // 刷新详
                var detail = await DbHelper.Db.Works.AsNoTracking()
                    .Include(w => w.Expenses)
                    .ThenInclude(e => e.Contact)
                    .Include(w => w.WorkContacts)
                    .ThenInclude(wc => wc.Contact)
                    .FirstOrDefaultAsync(w => w.Id == WorkDetails.Id);

                if (detail != null)
                {
                    WorkDetails = detail;
                    var msg = LangCombService.Succerss(Lang.Resources.Contact, workContact.Contact.Name, true);
                    ShowNotification(Lang.Resources.Success, msg, NotificationType.Success);
                }

                UpdateWorkCalculations();
            }
        }
        catch (Exception ex)
        {
            ShowNotification(Lang.Resources.Error, ex.Message, NotificationType.Error);
        }
    }

    private void UpdateWorkCalculations()
    {
        try
        {
            if (WorkDetails?.Expenses == null)
            {
                ReceivingPayment = 0;
                Cost = 0;
                return;
            }

            ReceivingPayment = WorkDetails.Expenses.Where(e => e.Income).Sum(e => e.Amount);
            Cost = WorkDetails.Expenses.Where(e => !e.Income).Sum(e => e.Amount);
        }
        catch (Exception ex)
        {
            ShowNotification(Lang.Resources.Error, Lang.Resources.CalculationError, NotificationType.Error);
            ReceivingPayment = 0;
            Cost = 0;
        }
    }
}