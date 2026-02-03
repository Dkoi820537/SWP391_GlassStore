using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Models;

/// <summary>
/// Bundle entity - inherits from Product (TPT inheritance).
/// Maps to the 'bundles' table.
/// </summary>
public class Bundle : Product
{
    public string? BundleNote { get; set; }

    // Navigation properties
    public virtual ICollection<BundleItem> BundleItems { get; set; } = new List<BundleItem>();
}
