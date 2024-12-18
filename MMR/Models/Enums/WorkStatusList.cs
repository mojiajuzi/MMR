using System;
using System.Collections.Generic;
using System.Linq;
using MMR.Lang;

namespace MMR.Models.Enums;

public class WorkStatusItem
{
    public WorkStatus? Status { get; set; }
    public string Text { get; set; }

    public WorkStatusItem(WorkStatus? status)
    {
        Status = status;
        Text = status.HasValue ? status.Value switch
        {
            WorkStatus.PreStart => Resources.WorkPreStart,
            WorkStatus.Start => Resources.WorkStart,
            WorkStatus.Running => Resources.WorkRunning,
            WorkStatus.End => Resources.WorkEnd,
            WorkStatus.Acceptance => Resources.WorkAcceptance,
            WorkStatus.Cancel => Resources.WorkCancel,
            WorkStatus.Archive => Resources.WorkArchive,
            _ => status.ToString()!
        } : Resources.All;  // 这里需要在Resources中添加All的翻译
    }
}

public static class WorkStatusListExtensions
{
    public static List<WorkStatusItem> GetStatusList()
    {
        var list = new List<WorkStatusItem> { new WorkStatusItem(null) };  // 添加"全部"选项
        list.AddRange(Enum.GetValues<WorkStatus>().Select(s => new WorkStatusItem(s)));
        return list;
    }
} 