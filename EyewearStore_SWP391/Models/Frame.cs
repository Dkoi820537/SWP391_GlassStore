using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Models;

public partial class Frame
{
    public int FrameId { get; set; }

    public string Name { get; set; } = null!;

    public string? FrameType { get; set; }

    public string? Material { get; set; }

    public string? Color { get; set; }

    public int? SizeWidth { get; set; }

    public int? SizeBridge { get; set; }

    public int? SizeTemple { get; set; }

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    public string? StockStatus { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
