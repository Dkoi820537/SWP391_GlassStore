using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Models;

public partial class Shipment
{
    public int ShipmentId { get; set; }

    public int OrderId { get; set; }

    public string? Carrier { get; set; }

    public string? TrackingNumber { get; set; }

    public string? TrackingUrl { get; set; }

    public string? Status { get; set; }

    public DateTime? ShippedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual ICollection<ShipmentStatusHistory> ShipmentStatusHistories { get; set; } = new List<ShipmentStatusHistory>();
}
