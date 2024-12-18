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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Skia;
using Avalonia.Styling;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using MMR.Models;

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
    [ObservableProperty] private SolidColorPaint _legendTextPaintColor;

    public DashboardViewModel()
    {
        LoadStatistics();
        LoadCharts();
        LoadRecentActivities();

        // 初始化图例文字颜色
        UpdateLegendTextColor();

        // 订阅主题变化事件
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += (s, e) => { UpdateLegendTextColor(); };
        }
    }

    // 更新图例文字颜色
    private void UpdateLegendTextColor()
    {
        try
        {
            if (Application.Current?.FindResource("PrimaryBrush") is SolidColorBrush brush)
            {
                LegendTextPaintColor = new SolidColorPaint(brush.Color.ToSKColor());
            }
            else
            {
                // 如果找不到资源，使用默认颜色
                var defaultColor = Application.Current?.FindResource("ForegroundColor") as Color? ?? Colors.Gray;
                LegendTextPaintColor = new SolidColorPaint(defaultColor.ToSKColor());
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating legend text color: {ex}");
            // 使用默认颜色
            LegendTextPaintColor = new SolidColorPaint(SKColors.Gray);
        }
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
        try
        {
            var works = DbHelper.Db.Works.AsNoTracking().ToList();
            var statusGroups = works.GroupBy(w => w.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToList();

            var values = new List<double>();
            var labels = new List<string>();

            // 使用WorkStatusItem来获取本地化的状态文本
            foreach (WorkStatus status in Enum.GetValues<WorkStatus>())
            {
                var count = statusGroups.FirstOrDefault(x => x.Status == status)?.Count ?? 0;
                values.Add(count);
                // 使用WorkStatusItem获取本地化文本
                labels.Add(new WorkStatusItem(status).Text);
            }

            // 使用Nord.axaml中定义的颜色
            var colorKeys = new[]
            {
                "Nord11", // 红色
                "Nord12", // 橙色
                "Nord13", // 黄色
                "Nord14", // 绿色
                "Nord15", // 紫色
                "Nord7", // 青绿色
                "Nord8" // 浅蓝色
            };

            var series = new List<ISeries>();

            // 获取前景色用于图例文字
            var foregroundColor = TryGetResourceColor("ForegroundColor");

            // 为每个状态创建一个饼图系列
            for (int i = 0; i < values.Count; i++)
            {
                var colorKey = colorKeys[i % colorKeys.Length];
                var color = TryGetResourceColor(colorKey);

                series.Add(new PieSeries<double>
                {
                    Values = new[] { values[i] },
                    Name = labels[i],
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                    Fill = new SolidColorPaint(color),
                });
            }

            WorkStatusSeries = series.ToArray();
        }
        catch (Exception ex)
        {
            ShowNotification(Lang.Resources.Error, Lang.Resources.LoadError, NotificationType.Error);
            System.Diagnostics.Debug.WriteLine($"LoadWorkStatusChart error: {ex}");
        }
    }

    // 添加一个安全的颜色获取方法
    private SKColor TryGetResourceColor(string resourceKey)
    {
        try
        {
            if (Application.Current?.FindResource(resourceKey) is Color color)
            {
                return color.ToSKColor();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting color resource {resourceKey}: {ex}");
        }

        // 如果获取失败，返回默认颜色
        return SKColors.Gray;
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

        // 获取Nord主题颜色
        var successColor = TryGetResourceColor("Nord14"); // 绿色
        var warningColor = TryGetResourceColor("Nord11"); // 红色

        MonthlyExpenseSeries = new ISeries[]
        {
            new LineSeries<decimal>
            {
                Values = incomeData.ToList(),
                Name = Lang.Resources.Income,
                Stroke = new SolidColorPaint(warningColor),
                GeometryStroke = new SolidColorPaint(warningColor),
                GeometrySize = 8,
                DataLabelsFormatter = (point) => $"{Lang.Resources.Income}: {point.Model:C2}"
            },
            new LineSeries<decimal>
            {
                Values = expenseData.ToList(),
                Name = Lang.Resources.Expenditure,
                Stroke = new SolidColorPaint(successColor),
                GeometryStroke = new SolidColorPaint(successColor),
                GeometrySize = 8,
                DataLabelsFormatter = (point) => $"{Lang.Resources.Expense}: {point.Model:C2}"
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
                Title = e.Income ? Lang.Resources.IncomeRecord : Lang.Resources.ExpenseRecord,
                Description = $"{e.Contact.Name} - {e.Work.Name}",
                Amount = e.Amount,
                IsIncome = e.Income
            });

        // 获取最近的工状态变更
        var recentWorks = DbHelper.Db.Works
            .OrderByDescending(w => w.UpdatedAt)
            .Take(5)
            .AsNoTracking()
            .ToList() // 先获取数据到内存
            .Select(w => new RecentActivity
            {
                Time = w.UpdatedAt,
                Title = Lang.Resources.WorkStatusUpdate,
                Description = $"{w.Name} - {w.StatusText}",
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