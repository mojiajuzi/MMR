using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MMR.Lang;

namespace MMR.Models;

public class Tag : BaseModel
{
    [Required(ErrorMessageResourceType = typeof(Resources), 
              ErrorMessageResourceName = "TagNameRequired")]
    [StringLength(50, MinimumLength = 2, 
                 ErrorMessageResourceType = typeof(Resources),
                 ErrorMessageResourceName = "TagNameLength")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessageResourceType = typeof(Resources),
             ErrorMessageResourceName = "Required")]
    public bool IsActive { get; set; }

    // 导航属性
    public virtual ICollection<ContactTag> ContactTags { get; set; } = new List<ContactTag>();
}