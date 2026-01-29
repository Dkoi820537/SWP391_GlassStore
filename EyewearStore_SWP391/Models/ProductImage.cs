using System;

namespace EyewearStore_SWP391.Models;

/// <summary>
/// Link table entity connecting products to images.
/// Allows multiple images per product with primary image designation.
/// </summary>
public partial class ProductImage
{
    public int ProductImageId { get; set; }

    public int ProductId { get; set; }

    public int ImageId { get; set; }

    public bool IsPrimary { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation property to the Image entity
    public virtual Image Image { get; set; } = null!;
}
