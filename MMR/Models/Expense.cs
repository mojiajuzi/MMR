using System;
using System.ComponentModel.DataAnnotations;

namespace MMR.Models;

public class Expense : BaseModel
{
    [Required(ErrorMessage = "金额不能为空")]
    [Range(0.01, double.MaxValue, ErrorMessage = "金额必须大于0")]
    [DataType(DataType.Currency, ErrorMessage = "金额格式不正确")]
    [DisplayFormat(DataFormatString = "{0:C2}", ApplyFormatInEditMode = true)]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "日期不能为空")]
    [DataType(DataType.DateTime, ErrorMessage = "日期格式不正确")]
    public DateTime Date { get; set; }

    [Required(ErrorMessage = "收支类型不能为空")]
    public bool Income { get; set; }

    [Required(ErrorMessage = "备注不能为空")]
    [MaxLength(500, ErrorMessage = "备注长度不能超过500个字符")]
    public string? Notes { get; set; }

    [Required(ErrorMessage = "联系人不能为空")]
    public int ContactId { get; set; }
    public virtual Contact Contact { get; set; }

    [Required(ErrorMessage = "工作项目不能为空")]
    public int WorkId { get; set; }
    public virtual Work Work { get; set; }
}