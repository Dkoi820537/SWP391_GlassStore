using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Models;

/// <summary>
/// Universal image entity for storing all image assets.
/// Supports product images, banners, logos, promotions, and category images.
/// </summary>
public partial class Image
{
    public int ImageId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public string? AltText { get; set; }

    public string ImageType { get; set; } = null!;

    public string? Context { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? LinkUrl { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // Navigation property for product images link
    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
}
