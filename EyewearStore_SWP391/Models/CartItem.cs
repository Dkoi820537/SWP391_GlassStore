using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Models;

public partial class CartItem
{
    public int CartItemId { get; set; }

    public int CartId { get; set; }

    public int? BundleId { get; set; }

    public int? FrameId { get; set; }

    public int? LensId { get; set; }

    public int? ServiceId { get; set; }

    public int Quantity { get; set; }

    public bool IsBundle { get; set; }

    public string? TempPrescriptionJson { get; set; }

    public virtual Bundle? Bundle { get; set; }

    public virtual Cart Cart { get; set; } = null!;

    public virtual Frame? Frame { get; set; }

    public virtual Lense? Lens { get; set; }

    public virtual Service? Service { get; set; }
}
