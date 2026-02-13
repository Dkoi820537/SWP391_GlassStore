using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace EyewearStore_SWP391.Models;

[Table("order_items", Schema = "dbo")]
public partial class OrderItem
{
    [Column("order_item_id")]
    public int OrderItemId { get; set; }

    [Column("order_id")]
    public int OrderId { get; set; }

    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("prescription_id")]
    public int? PrescriptionId { get; set; }

    [Column("unit_price")]
    public decimal UnitPrice { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("is_bundle")]
    public bool IsBundle { get; set; }

    [Column("snapshot_json")]
    public string? SnapshotJson { get; set; }

    // Navigation properties
    public virtual Order Order { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
    public virtual PrescriptionProfile? Prescription { get; set; }
    public virtual ICollection<Return> Returns { get; set; } = new List<Return>();
}