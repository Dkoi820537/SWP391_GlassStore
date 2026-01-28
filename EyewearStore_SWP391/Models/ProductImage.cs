using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Models;

public partial class ProductImage
{
    public int ImageId { get; set; }

    public string ProductType { get; set; } = null!;

    public int ProductId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public bool IsPrimary { get; set; }

    public DateTime CreatedAt { get; set; }
}
