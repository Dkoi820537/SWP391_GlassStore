using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace EyewearStore_SWP391.Models;

[Table("returns", Schema = "dbo")]
public partial class Return
{
    [Column("return_id")]
    public int ReturnId { get; set; }

    [Column("order_item_id")]
    public int OrderItemId { get; set; }

    [Column("user_id")]
    public int? UserId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; } = 1;

    [Column("return_type")]
    public string? ReturnType { get; set; }

    [Column("reason_category")]
    public string? ReasonCategory { get; set; }

    [Column("reason")]
    public string? Reason { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("image_urls")]
    public string? ImageUrls { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Pending";

    [Column("refund_amount")]
    public decimal? RefundAmount { get; set; }

    [Column("reviewed_by")]
    public int? ReviewedBy { get; set; }

    [Column("reviewed_at")]
    public DateTime? ReviewedAt { get; set; }

    [Column("staff_notes")]
    public string? StaffNotes { get; set; }

    [Column("rejection_reason")]
    public string? RejectionReason { get; set; }

    [Column("return_tracking_number")]
    public string? ReturnTrackingNumber { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    public virtual OrderItem OrderItem { get; set; } = null!;
    public virtual User? User { get; set; }

    // ❌ REMOVE Reviewer navigation - causes FK conflict
    // We'll use ReviewedBy (int) directly and query when needed
}