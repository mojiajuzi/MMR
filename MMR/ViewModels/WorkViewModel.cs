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
using MMR.Data;
using MMR.Models;
using MMR.Models.Enums;
using MMR.Components.Popups.AddContact;

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

    private readonly AddContactViewModel _addContactViewModel;

    [ObservableProperty] private bool _isContactPopupOpen;

    public AddContactViewModel AddContactViewModel => _addContactViewModel;

    public WorkViewModel(AddContactViewModel addContactViewModel)
    {
        _addContactViewModel = addContactViewModel;
        Works = new ObservableCollection<Work>(GetWorks());
        StatusList = new ObservableCollection<WorkStatus>(Enum.GetValues<WorkStatus>());

        // 订阅联系人添加事件
        _addContactViewModel.ContactAdded += OnContactAdded;
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
        if (WorkDetails == null) return;

        _addContactViewModel.Open(WorkDetails);
        IsContactPopupOpen = true;
    }

    private void OnContactAdded(object sender, WorkContact workContact)
    {
        //IsContactPopupOpen = false;

        if (workContact == null) return; // 用户取消

        // 刷新当前工作详情
        var detail = DbHelper.Db.Works.AsNoTracking()
            .Include(w => w.Expenses)
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
    }
}