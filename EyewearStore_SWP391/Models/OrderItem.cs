using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Models;

public partial class OrderItem
{
    public int OrderItemId { get; set; }

    public int OrderId { get; set; }

    public int? BundleId { get; set; }

    public int? FrameId { get; set; }

    public int? LensId { get; set; }

    public int? ServiceId { get; set; }

    public int? PrescriptionId { get; set; }

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public bool IsBundle { get; set; }

    public string? BundleSnapshot { get; set; }

    public virtual Bundle? Bundle { get; set; }

    public virtual Frame? Frame { get; set; }

    public virtual Lense? Lens { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual PrescriptionProfile? Prescription { get; set; }

    public virtual ICollection<Return> Returns { get; set; } = new List<Return>();

    public virtual Service? Service { get; set; }
}
