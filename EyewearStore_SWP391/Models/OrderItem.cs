using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Models;

public partial class OrderItem
{
    public int OrderItemId { get; set; }

    public int OrderId { get; set; }

    public int ProductId { get; set; }

    public int? PrescriptionId { get; set; }

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public bool IsBundle { get; set; }

    public string? SnapshotJson { get; set; }

    // Navigation properties
    public virtual Order Order { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;

    public virtual PrescriptionProfile? Prescription { get; set; }

    public virtual ICollection<Return> Returns { get; set; } = new List<Return>();
}
