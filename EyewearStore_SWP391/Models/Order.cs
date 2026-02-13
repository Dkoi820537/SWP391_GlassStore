using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace EyewearStore_SWP391.Models;

[Table("orders", Schema = "dbo")]
public partial class Order
{
    [Column("order_id")]
    public int OrderId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    // DB column is address_id (nullable based on your SELECT)
    [Column("address_id")]
    public int? AddressId { get; set; }

    // Snapshot fields present in DB (you SELECTed these)
    [Column("receiver_name")]
    public string ReceiverName { get; set; } = null!;

    [Column("phone")]
    public string Phone { get; set; } = null!;

    [Column("address_line")]
    public string AddressLine { get; set; } = null!;

    [Column("status")]
    public string Status { get; set; } = null!;

    [Column("total_amount")]
    public decimal TotalAmount { get; set; }

    [Column("payment_method")]
    public string? PaymentMethod { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("stripe_session_id")]
    public string? StripeSessionId { get; set; }

    [Column("stripe_payment_intent_id")]
    public string? StripePaymentIntentId { get; set; }

    // Navigation
    public virtual User User { get; set; } = null!;

    // Keep Address navigation if FK exists
    public virtual Address? Address { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public virtual ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}