using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Models;

public partial class CartItem
{
    public int CartItemId { get; set; }

    public int CartId { get; set; }

    public int ProductId { get; set; }

    public int? ServiceId { get; set; }

    public int Quantity { get; set; }

    public string? TempPrescriptionJson { get; set; }

    public decimal PrescriptionFee { get; set; }

    // Navigation properties
    public virtual Cart Cart { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;

    public virtual Service? Service { get; set; }
}
