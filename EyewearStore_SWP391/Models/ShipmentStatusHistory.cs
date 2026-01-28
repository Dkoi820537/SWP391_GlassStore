using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Models;

public partial class ShipmentStatusHistory
{
    public int HistoryId { get; set; }

    public int ShipmentId { get; set; }

    public string Status { get; set; } = null!;

    public string? StatusMessage { get; set; }

    public string? Location { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Shipment Shipment { get; set; } = null!;
}
