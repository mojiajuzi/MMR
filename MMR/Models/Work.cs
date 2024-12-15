using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using MMR.Models.Enums;

namespace MMR.Models;

public class Work : BaseModel
{
    [Required(ErrorMessage = "名称不能为空")]
    [MaxLength(100, ErrorMessage = "名称长度不能超过100个字符")]
    public string Name { get; set; } = null!;

    [MaxLength(500, ErrorMessage = "描述长度不能超过500个字符")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "开始时间不能为空")]
    [DataType(DataType.DateTime, ErrorMessage = "开始时间格式不正确")]
    public DateTime StartAt { get; set; } //开始时间

    [DataType(DataType.DateTime, ErrorMessage = "预期结束时间格式不正确")]
    [Compare(nameof(StartAt), ErrorMessage = "预期结束时间必须晚于开始时间")]
    public DateTime? EndAt { get; set; } //预期结束时间

    [DataType(DataType.DateTime, ErrorMessage = "实际结束时间格式不正确")]
    [Compare(nameof(StartAt), ErrorMessage = "实际结束时间必须晚于开始时间")]
    public DateTime? ExceptionAt { get; set; } //实际结束时间

    [Required(ErrorMessage = "总金额不能为空")]
    [Range(0, double.MaxValue, ErrorMessage = "总金额必须大于0")]
    [DataType(DataType.Currency, ErrorMessage = "总金额格式不正确")]
    [DisplayFormat(DataFormatString = "{0:C2}", ApplyFormatInEditMode = true)]
    public decimal? TotalMoney { get; set; } = 0; //总款项

    [Required(ErrorMessage = "状态不能为空")] public WorkStatus Status { get; set; } = WorkStatus.PreStart; //状态

    public ICollection<WorkContact> WorkContacts { get; set; } = new List<WorkContact>();
    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();

    // 不存储在数据库中的计算属性
    [NotMapped] public decimal ReceivingPayment => Expenses?.Where(e => e.Income).Sum(e => e.Amount) ?? 0;

    [NotMapped] public decimal Cost => Expenses?.Where(e => !e.Income).Sum(e => e.Amount) ?? 0;
}