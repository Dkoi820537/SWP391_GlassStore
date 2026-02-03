using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Models;

public partial class PrescriptionProfile
{
    public int PrescriptionId { get; set; }

    public int UserId { get; set; }

    public string? ProfileName { get; set; }

    public decimal? LeftSph { get; set; }

    public decimal? LeftCyl { get; set; }

    public int? LeftAxis { get; set; }

    public decimal? RightSph { get; set; }

    public decimal? RightCyl { get; set; }

    public int? RightAxis { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
