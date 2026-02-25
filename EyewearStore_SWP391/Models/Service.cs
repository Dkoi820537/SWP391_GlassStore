using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Models;

public partial class Service
{
    public int ServiceId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int? DurationMin { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}