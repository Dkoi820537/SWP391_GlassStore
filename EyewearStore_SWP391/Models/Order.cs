using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EyewearStore_SWP391.Models;

[Table("orders", Schema = "dbo")]
public partial class Order
{
    [Column("order_id")]
    public int OrderId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("address_id")]
    public int? AddressId { get; set; }

    [Column("receiver_name")]
    public string ReceiverName { get; set; } = null!;

    [Column("phone")]
    public string Phone { get; set; } = null!;

    [Column("address_line")]
    public string AddressLine { get; set; } = null!;

    [ConcurrencyCheck]
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

    /// <summary>
    /// GUID that links sibling orders created from the same checkout session.
    /// NULL if only one order was created (single-type cart).
    /// </summary>
    [Column("order_group_id")]
    public string? OrderGroupId { get; set; }

    /// <summary>
    /// "Standard" for off-the-shelf products, "Custom" for custom lens/service orders.
    /// </summary>
    [Column("order_type")]
    public string OrderType { get; set; } = "Standard";

    // ── COD Deposit fields ───────────────────────────────────────────────────

    /// <summary>
    /// Amount paid online as deposit. For COD = 50% of TotalAmount; for Stripe = TotalAmount.
    /// </summary>
    [Column("deposit_amount")]
    public decimal DepositAmount { get; set; }

    /// <summary>
    /// Remaining balance to be collected upon delivery. 0 for fully-paid orders.
    /// </summary>
    [Column("pending_balance")]
    public decimal PendingBalance { get; set; }

    /// <summary>
    /// "Pending" → "DepositPaid_AwaitingCOD" → "FullyPaid"
    /// </summary>
    [Column("payment_status")]
    public string PaymentStatus { get; set; } = "Pending";

    // ── Cancellation / Refund fields ────────────────────────────────────────

    /// <summary>
    /// GUID idempotency key sent to Stripe to prevent duplicate refunds.
    /// </summary>
    [Column("cancellation_idempotency_key")]
    public string? CancellationIdempotencyKey { get; set; }

    /// <summary>
    /// Amount actually refunded to the customer via Stripe.
    /// </summary>
    [Column("refund_amount")]
    public decimal? RefundAmount { get; set; }

    /// <summary>
    /// Timestamp when the order was cancelled.
    /// </summary>
    [Column("cancelled_at")]
    public DateTime? CancelledAt { get; set; }

    // Navigation
    public virtual User User { get; set; } = null!;
    public virtual Address? Address { get; set; }
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public virtual ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();

    // ── NEW: full status transition history ──────────────────────────────────
    public virtual ICollection<OrderStatusHistory> StatusHistories { get; set; }
        = new List<OrderStatusHistory>();
}