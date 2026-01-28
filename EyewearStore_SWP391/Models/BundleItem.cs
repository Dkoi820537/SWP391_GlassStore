using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Models;

public partial class BundleItem
{
    public int BundleItemId { get; set; }

    public int BundleId { get; set; }

    public string ItemType { get; set; } = null!;

    public int ItemId { get; set; }

    public bool IsRequired { get; set; }

    public int Quantity { get; set; }

    public virtual Bundle Bundle { get; set; } = null!;
}
