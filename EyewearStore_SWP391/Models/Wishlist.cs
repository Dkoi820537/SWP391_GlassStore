using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Models;

/// <summary>
/// Wishlist entity - tracks user wishlisted products.
/// Maps to the 'wishlist' table.
/// </summary>
public class Wishlist
{
    public int WishlistId { get; set; }

    public int UserId { get; set; }

    public int ProductId { get; set; }

    public bool NotifyOnRestock { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
