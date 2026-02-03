using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Models;

public partial class Return
{
    public int ReturnId { get; set; }

    public int OrderItemId { get; set; }

    public string? Reason { get; set; }

    public string? Status { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public virtual OrderItem OrderItem { get; set; } = null!;
}
