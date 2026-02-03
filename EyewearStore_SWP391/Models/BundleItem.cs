using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Models;

public partial class BundleItem
{
    public int BundleItemId { get; set; }

    public int BundleProductId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    // Navigation properties
    public virtual Bundle BundleProduct { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
