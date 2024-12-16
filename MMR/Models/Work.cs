using MMR.Lang;
using MMR.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace MMR.Models;

public class Work : BaseModel
{
    [Required(ErrorMessageResourceType = typeof(Resources),
              ErrorMessageResourceName = "WorkNameRequired")]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    [Required(ErrorMessageResourceType = typeof(Resources),
              ErrorMessageResourceName = "WorkStartTimeRequired")]
    [DataType(DataType.DateTime, ErrorMessageResourceType = typeof(Resources),
              ErrorMessageResourceName = "WorkStartTimeInvalid")]
    public DateTime StartAt { get; set; } //开始时间

    [DataType(DataType.DateTime, ErrorMessageResourceType = typeof(Resources),
              ErrorMessageResourceName = "WorkEndTimeInvalid")]
    [Compare(nameof(StartAt), ErrorMessageResourceType = typeof(Resources),
             ErrorMessageResourceName = "WorkEndTimeMustLaterThanStart")]
    public DateTime? EndAt { get; set; } //预期结束时间

    [DataType(DataType.DateTime, ErrorMessageResourceType = typeof(Resources),
              ErrorMessageResourceName = "WorkExceptionTimeInvalid")]
    [Compare(nameof(StartAt), ErrorMessageResourceType = typeof(Resources),
             ErrorMessageResourceName = "WorkExceptionTimeMustLaterThanStart")]
    public DateTime? ExceptionAt { get; set; } //实际结束时间

    [Required(ErrorMessageResourceType = typeof(Resources),
              ErrorMessageResourceName = "WorkTotalMoneyRequired")]
    [DisplayFormat(DataFormatString = "{0:C2}", ApplyFormatInEditMode = true)]
    public decimal? TotalMoney { get; set; } = 0; //总款项

    [Required(ErrorMessageResourceType = typeof(Resources),
              ErrorMessageResourceName = "WorkStatusRequired")]
    public WorkStatus Status { get; set; } = WorkStatus.PreStart; //状态

    public ICollection<WorkContact> WorkContacts { get; set; } = new List<WorkContact>();
    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();

    // 不存储在数据库中的计算属性
    [NotMapped] public decimal ReceivingPayment => Expenses?.Where(e => e.Income).Sum(e => e.Amount) ?? 0;
    [NotMapped] public decimal Cost => Expenses?.Where(e => !e.Income).Sum(e => e.Amount) ?? 0;
}