using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Models;

public partial class StockNotification
{
    public int NotificationId { get; set; }

    public string ProductType { get; set; } = null!;

    public int ProductId { get; set; }

    public string Email { get; set; } = null!;

    public string? Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? NotifiedAt { get; set; }
}
