using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Models;

public partial class Service
{
    public int ServiceId { get; set; }

    public string Name { get; set; } = null!;

    public decimal Price { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}
