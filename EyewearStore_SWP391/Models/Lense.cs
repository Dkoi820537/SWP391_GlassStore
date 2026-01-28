using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Models;

public partial class Lense
{
    public int LensId { get; set; }

    public string LensType { get; set; } = null!;

    public decimal? IndexValue { get; set; }

    public string? Coating { get; set; }

    public decimal Price { get; set; }

    public string? StockStatus { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
