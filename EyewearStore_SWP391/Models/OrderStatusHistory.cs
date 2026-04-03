using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace EyewearStore_SWP391.Models;

/// <summary>
/// Tracks every status transition for an Order with timestamp and actor info.
/// One row per transition — used to render the customer-facing timeline.
/// </summary>
[Table("order_status_history")]
public class OrderStatusHistory
{
    [Column("history_id")]
    public int HistoryId { get; set; }

    [Column("order_id")]
    public int OrderId { get; set; }

    /// <summary>The status this order moved TO.</summary>
    [Column("status")]
    public string Status { get; set; } = "";

    /// <summary>Who triggered the change: "Customer", "Sale/Support", "Operations", "Admin", "System"</summary>
    [Column("actor")]
    public string Actor { get; set; } = "System";

    /// <summary>Optional note, e.g. cancellation reason or admin override note.</summary>
    [Column("note")]
    public string? Note { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual Order Order { get; set; } = null!;
}