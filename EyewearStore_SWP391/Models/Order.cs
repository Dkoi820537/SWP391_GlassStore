using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int UserId { get; set; }

    public int AddressId { get; set; }

    public string Status { get; set; } = null!;

    public decimal TotalAmount { get; set; }

    public string? PaymentMethod { get; set; }

    /// <summary>Stripe Checkout Session ID (cs_test_...)</summary>
    public string? StripeSessionId { get; set; }

    /// <summary>Stripe Payment Intent ID (pi_...)</summary>
    public string? StripePaymentIntentId { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;

    public virtual Address Address { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}
