using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int UserId { get; set; }

    public int AddressId { get; set; }

    public string? OrderType { get; set; }

    public decimal TotalAmount { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? ShippingStatus { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? ShippingReceiverName { get; set; }

    public string? ShippingPhone { get; set; }

    public virtual Address Address { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();

    public virtual User User { get; set; } = null!;
}
