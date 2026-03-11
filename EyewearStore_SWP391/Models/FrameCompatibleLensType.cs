namespace EyewearStore_SWP391.Models;

/// <summary>
/// Join entity mapping a Frame to a compatible lens type (category-based).
/// Composite PK: (FrameProductId, LensType).
/// </summary>
public class FrameCompatibleLensType
{
    public int FrameProductId { get; set; }
    public string LensType { get; set; } = null!;

    // Navigation
    public virtual Frame Frame { get; set; } = null!;
}
