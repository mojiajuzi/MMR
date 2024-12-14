using System;
using System.ComponentModel.DataAnnotations;

namespace MMR.Models;

public class WorkContact
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "工作项目不能为空")]
    public int WorkId { get; set; }
    public virtual Work Work { get; set; }

    [Key]
    public int ContactId { get; set; }
    public virtual Contact Contact { get; set; }

    public decimal Money { get; set; }

    public int Income { get; set; }

    [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}