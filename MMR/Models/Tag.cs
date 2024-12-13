using System.ComponentModel.DataAnnotations;

namespace MMR.Models;

public class Tag : BaseModel
{
    [Required(ErrorMessage = "名称不能为空")]
    [MaxLength(50, ErrorMessage = "名称长度不能超过50个字符")]
    public string Name { get; set; }
    
    [Required(ErrorMessage = "状态不能为空")]
    public bool IsActive { get; set; }
}