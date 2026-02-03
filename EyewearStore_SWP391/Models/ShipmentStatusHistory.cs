using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Models;

public partial class ShipmentStatusHistory
{
    public int HistoryId { get; set; }

    public int ShipmentId { get; set; }

    public string? Status { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public virtual Shipment Shipment { get; set; } = null!;
}
