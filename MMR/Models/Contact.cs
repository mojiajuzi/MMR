using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MMR.Lang;

namespace MMR.Models;

public class Contact : BaseModel
{
    [Required(ErrorMessageResourceType = typeof(Resources), 
              ErrorMessageResourceName = "ContactNameRequired")]
    [StringLength(50, MinimumLength = 2, 
                 ErrorMessageResourceType = typeof(Resources),
                 ErrorMessageResourceName = "ContactNameLength")]
    public string Name { get; set; } = null!;

    [EmailAddress(ErrorMessageResourceType = typeof(Resources),
                 ErrorMessageResourceName = "ContactEmailInvalid")]
    [StringLength(100, 
                 ErrorMessageResourceType = typeof(Resources),
                 ErrorMessageResourceName = "StringLength")]
    public string? Email { get; set; }

    [RegularExpression(@"^\d{11}$", 
                      ErrorMessageResourceType = typeof(Resources),
                      ErrorMessageResourceName = "ContactPhoneInvalid")]
    [StringLength(20, 
                 ErrorMessageResourceType = typeof(Resources),
                 ErrorMessageResourceName = "StringLength")]
    public string? Phone { get; set; }

    [StringLength(50, 
                 ErrorMessageResourceType = typeof(Resources),
                 ErrorMessageResourceName = "StringLength")]
    public string? Wechat { get; set; }

    [StringLength(20, 
                 ErrorMessageResourceType = typeof(Resources),
                 ErrorMessageResourceName = "StringLength")]
    [RegularExpression(@"^\d{5,11}$", 
                      ErrorMessageResourceType = typeof(Resources),
                      ErrorMessageResourceName = "QQInvalid")]
    public string? QQ { get; set; }

    [StringLength(500, 
                 ErrorMessageResourceType = typeof(Resources),
                 ErrorMessageResourceName = "StringLength")]
    public string? Remark { get; set; }

    [Required(ErrorMessageResourceType = typeof(Resources),
             ErrorMessageResourceName = "Required")]
    public bool IsActive { get; set; }

    [StringLength(500, 
                 ErrorMessageResourceType = typeof(Resources),
                 ErrorMessageResourceName = "StringLength")]
    public string? Avatar { get; set; }

    // 导航属性
    public ICollection<ContactTag> ContactTags { get; set; } = new List<ContactTag>();
    public virtual ICollection<WorkContact> WorkContacts { get; set; } = new List<WorkContact>();
    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}