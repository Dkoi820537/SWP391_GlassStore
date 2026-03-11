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

    // ── Fields added in v2 ───────────────────────────────────────────────────
    /// <summary>Brand / manufacturer (e.g. Ray-Ban, Bolon, Molsion)</summary>
    public string? Brand { get; set; }
    /// <summary>Frame colour(s), comma-separated (e.g. "Black, Gold, Red")</summary>
    public string? Color { get; set; }
    /// <summary>Target gender: Unisex | Male | Female</summary>
    public string? Gender { get; set; }
    /// <summary>Lens/frame shape: Round | Square | Rectangle | Aviator | Cat-eye | Oval</summary>
    public string? FrameShape { get; set; }

    // ── Fields added in v3 ───────────────────────────────────────────────────
    /// <summary>Lens width in mm (e.g. 53)</summary>
    public decimal? LensWidth { get; set; }
    /// <summary>Country of origin (e.g. P.R.C, Japan, Italy)</summary>
    public string? Origin { get; set; }
}