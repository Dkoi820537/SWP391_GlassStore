using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Models;

public partial class Bundle
{
    public int BundleId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal BundlePrice { get; set; }

    public bool IsActive { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<BundleItem> BundleItems { get; set; } = new List<BundleItem>();

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
