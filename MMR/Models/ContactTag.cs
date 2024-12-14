using System;
using System.ComponentModel.DataAnnotations;

namespace MMR.Models;

public class ContactTag
{
    public int ContactId { get; set; }
    public virtual Contact Contact { get; set; } = null!;

    public int TagId { get; set; }
    public virtual Tag Tag { get; set; } = null!;

    [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}