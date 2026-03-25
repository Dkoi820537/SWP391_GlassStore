using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Models;

public class Frame : Product
{
    public string? FrameMaterial { get; set; }
    public string? FrameType { get; set; }
    public decimal? BridgeWidth { get; set; }
    public decimal? TempleLength { get; set; }

    // ── v2 ──────────────────────────────────────────────────────────────────
    public string? Brand { get; set; }
    public string? Color { get; set; }
    public string? Gender { get; set; }
    public string? FrameShape { get; set; }

    // ── v3 ──────────────────────────────────────────────────────────────────
    public decimal? LensWidth { get; set; }
    public string? Origin { get; set; }

    // ── v4 ──────────────────────────────────────────────────────────────────
    /// <summary>Specific frame color e.g. "Vàng gold", "Đen bóng"</summary>
    public string? FrameColor { get; set; }

    /// <summary>Lens material e.g. Nylon, Polycarbonate, CR-39</summary>
    public string? LensMaterial { get; set; }

    /// <summary>Lens color e.g. "Xám đậm", "Nâu"</summary>
    public string? LensColor { get; set; }

    /// <summary>Suitable face shapes comma-separated e.g. "Mặt oval, Mặt tròn"</summary>
    public string? SuitableFaceShapes { get; set; }

    /// <summary>Whether lens is polarized</summary>
    public bool? IsPolarized { get; set; }

    /// <summary>Whether lens has UV protection</summary>
    public bool? HasUvProtection { get; set; }

    /// <summary>Style tags comma-separated e.g. "Vintage, Sporty"</summary>
    public string? StyleTags { get; set; }

    // ── Compatibility ────────────────────────────────────────────────────────
    public virtual ICollection<FrameCompatibleLensType> CompatibleLensTypes { get; set; } = new List<FrameCompatibleLensType>();
}