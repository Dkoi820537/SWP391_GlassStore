using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Models;

/// <summary>
/// Lens entity - inherits from Product (TPT inheritance).
/// Maps to the 'lenses' table.
/// </summary>
public class Lens : Product
{
    public string? LensType { get; set; }

    public decimal? LensIndex { get; set; }

    public bool IsPrescription { get; set; }
}
