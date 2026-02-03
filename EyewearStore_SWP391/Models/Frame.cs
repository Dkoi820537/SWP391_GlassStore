using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Models;

/// <summary>
/// Frame entity - inherits from Product (TPT inheritance).
/// Maps to the 'frames' table.
/// </summary>
public class Frame : Product
{
    public string? FrameMaterial { get; set; }

    public string? FrameType { get; set; }

    public decimal? BridgeWidth { get; set; }

    public decimal? TempleLength { get; set; }
}
