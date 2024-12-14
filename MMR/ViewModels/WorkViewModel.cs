using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
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

    public WorkViewModel()
    {
        Works = new ObservableCollection<Work>(GetWorks());
        // 初始化状态列表
        StatusList = new ObservableCollection<WorkStatus>(Enum.GetValues<WorkStatus>());
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
        IsDetailsPaneOpen = true;

        var detail = DbHelper.Db.Works.AsNoTracking()
            .Include(w => w.Expenses)
            .Include(w => w.WorkContacts)
            .ThenInclude(wc => wc.Contact)
            .FirstOrDefault(w => w.Id == work.Id);

        if (detail == null) return;
        WorkDetails = detail;
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
    }

    [RelayCommand]
    private void AddExpense()
    {
    }
}