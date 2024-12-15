using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using MMR.Data;
using MMR.Models.Enums;
using MMR.ViewModels;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;

namespace MMR.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    [ObservableProperty] private int _totalWorks;
    [ObservableProperty] private int _runningWorks;
    [ObservableProperty] private int _completedWorks;
    [ObservableProperty] private decimal _totalIncome;
    [ObservableProperty] private decimal _totalExpense;
    [ObservableProperty] private decimal _monthIncome;
    [ObservableProperty] private decimal _monthExpense;
    [ObservableProperty] private ISeries[] _workStatusSeries;
    [ObservableProperty] private ISeries[] _monthlyExpenseSeries;
    [ObservableProperty] private ObservableCollection<RecentActivity> _recentActivities;
    [ObservableProperty] private decimal _estimatedTotalIncome;

    public DashboardViewModel()
    {
        LoadStatistics();
        LoadCharts();
        LoadRecentActivities();
    }

    private void LoadStatistics()
    {
        var now = DateTime.Now;
        var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);

        // 获取工作统计
        var works = DbHelper.Db.Works.AsNoTracking().ToList();
        TotalWorks = works.Count;
        RunningWorks = works.Count(w => w.Status == WorkStatus.Running);
        CompletedWorks = works.Count(w => w.Status == WorkStatus.End);

        // 获取收支统计
        var expenses = DbHelper.Db.Expenses.AsNoTracking().ToList();
        TotalIncome = expenses.Where(e => e.Income).Sum(e => e.Amount);
        TotalExpense = expenses.Where(e => !e.Income).Sum(e => e.Amount);
        MonthIncome = expenses.Where(e => e.Income && e.Date >= firstDayOfMonth).Sum(e => e.Amount);
        MonthExpense = expenses.Where(e => !e.Income && e.Date >= firstDayOfMonth).Sum(e => e.Amount);

        // 计算预计总收入
        EstimatedTotalIncome = (decimal)works.Sum(w => w.TotalMoney);
    }

    private void LoadCharts()
    {
        LoadWorkStatusChart();
        LoadMonthlyExpenseChart();
        LoadContactExpenseChart();
    }

    private void LoadWorkStatusChart()
    {
        var works = DbHelper.Db.Works.AsNoTracking().ToList();
        var statusGroups = works.GroupBy(w => w.Status)
            .Select(g => new { Status = GetStatusName(g.Key), Count = g.Count() })
            .ToList();

        WorkStatusSeries = new ISeries[]
        {
            new PieSeries<int>
            {
                Values = statusGroups.Select(x => x.Count).ToList(),
                Name = "工作状态分布",
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                DataLabelsFormatter = point => $"{statusGroups[point.Index].Status}: {point.Coordinate.PrimaryValue}"
            }
        };
    }

    private string GetStatusName(WorkStatus status)
    {
        return status switch
        {
            WorkStatus.PreStart => "未开始",
            WorkStatus.Start => "已开始",
            WorkStatus.Running => "进行中",
            WorkStatus.End => "已结束",
            WorkStatus.Acceptance => "验收中",
            WorkStatus.Cancel => "已��消",
            WorkStatus.Archive => "已归档",
            _ => status.ToString()
        };
    }

    private void LoadMonthlyExpenseChart()
    {
        var now = DateTime.Now;
        var startDate = now.AddMonths(-5);
        var expenses = DbHelper.Db.Expenses
            .Where(e => e.Date >= startDate)
            .AsNoTracking()
            .ToList();

        var monthlyData = expenses
            .GroupBy(e => new { e.Date.Year, e.Date.Month, e.Income })
            .Select(g => new
            {
                Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                Amount = g.Sum(e => e.Amount),
                IsIncome = g.Key.Income
            })
            .OrderBy(x => x.Date)
            .ToList();

        var incomeData = new List<decimal>();
        var expenseData = new List<decimal>();
        var labels = new List<string>();

        for (var date = startDate; date <= now; date = date.AddMonths(1))
        {
            var monthData = monthlyData.Where(x => x.Date.Year == date.Year && x.Date.Month == date.Month);
            incomeData.Add(monthData.Where(x => x.IsIncome).Sum(x => x.Amount));
            expenseData.Add(monthData.Where(x => !x.IsIncome).Sum(x => x.Amount));
            labels.Add(date.ToString("yyyy-MM"));
        }

        MonthlyExpenseSeries = new ISeries[]
        {
            new LineSeries<decimal>
            {
                Values = incomeData.ToList(),
                Name = "Income",
                Stroke = new SolidColorPaint(SKColors.Green),
                GeometryStroke = new SolidColorPaint(SKColors.Green),
                GeometrySize = 8,
                DataLabelsFormatter = (point) => $"收入: {point.Model:C2}"
            },
            new LineSeries<decimal>
            {
                Values = expenseData.ToList(),
                Name = "Expense",
                Stroke = new SolidColorPaint(SKColors.Red),
                GeometryStroke = new SolidColorPaint(SKColors.Red),
                GeometrySize = 8,
                DataLabelsFormatter = (point) => $"支出: {point.Model:C2}"
            }
        };
    }

    private void LoadContactExpenseChart()
    {
        // 先获取数据，然后在内存中排序
        var topContacts = DbHelper.Db.Expenses
            .Include(e => e.Contact)
            .AsNoTracking()
            .ToList() // 先加载到内存
            .GroupBy(e => e.Contact)
            .Select(g => new
            {
                Contact = g.Key,
                Amount = g.Sum(e => e.Amount)
            })
            .OrderByDescending(x => x.Amount) // 在内存中排序
            .Take(5)
            .ToList();
    }

    private void LoadRecentActivities()
    {
        // 获取最近的支出记录
        var recentExpenses = DbHelper.Db.Expenses
            .Include(e => e.Contact)
            .Include(e => e.Work)
            .OrderByDescending(e => e.CreatedAt)
            .Take(5)
            .AsNoTracking()
            .ToList() // 先获取数据到内存
            .Select(e => new RecentActivity
            {
                Time = e.CreatedAt,
                Title = e.Income ? "收入记录" : "支出记录",
                Description = $"{e.Contact.Name} - {e.Work.Name}",
                Amount = e.Amount,
                IsIncome = e.Income
            });

        // 获取最近的工作状态变更
        var recentWorks = DbHelper.Db.Works
            .OrderByDescending(w => w.UpdatedAt)
            .Take(5)
            .AsNoTracking()
            .ToList() // 先获取数据到内存
            .Select(w => new RecentActivity
            {
                Time = w.UpdatedAt,
                Title = "工作状态更新",
                Description = $"{w.Name} - {GetStatusName(w.Status)}",
                Amount = null,
                IsIncome = null
            });

        // 在内存中合并并排序
        var allActivities = recentExpenses.Concat(recentWorks)
            .OrderByDescending(a => a.Time)
            .Take(10)
            .ToList();

        RecentActivities = new ObservableCollection<RecentActivity>(allActivities);
    }
}

public class RecentActivity
{
    public DateTime Time { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public decimal? Amount { get; set; }
    public bool? IsIncome { get; set; }
}