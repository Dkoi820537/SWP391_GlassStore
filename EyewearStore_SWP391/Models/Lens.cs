using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Models;

/// <summary>
/// Lens entity - inherits from Product (TPT inheritance).
/// Maps to the 'lenses' table.
/// </summary>
public class Lens : Product
{
    // ── Brand & identity ─────────────────────────────────────────────────────

    /// <summary>Brand / manufacturer (e.g., Essilor, Hoya, Zeiss, Nikon)</summary>
    public string? Brand { get; set; }

    /// <summary>Country of origin (e.g., France, Germany, Japan)</summary>
    public string? Origin { get; set; }

    // ── Core lens specs ──────────────────────────────────────────────────────

    /// <summary>Lens type: Single Vision | Bifocal | Progressive | Reading</summary>
    public string? LensType { get; set; }

    /// <summary>Refractive index (e.g., 1.50, 1.60, 1.67, 1.74, 1.76)</summary>
    public decimal? LensIndex { get; set; }

    /// <summary>Whether a prescription is required for this lens</summary>
    public bool IsPrescription { get; set; }

    // ── Material & construction ──────────────────────────────────────────────

    /// <summary>Lens material: CR-39 | Polycarbonate | Trivex | MR-8 | Glass</summary>
    public string? LensMaterial { get; set; }

    /// <summary>Thickness category: Standard | Thin | Ultra-Thin | Aspheric</summary>
    public string? LensThickness { get; set; }

    // ── Coatings & treatments ────────────────────────────────────────────────

    /// <summary>Coatings, comma-separated (e.g., "Anti-Reflective, Anti-Scratch, Blue Light Filter")</summary>
    public string? LensCoating { get; set; }

    /// <summary>UV protection level: UV380 | UV400 | None</summary>
    public string? UVProtection { get; set; }

    public decimal? PrescriptionFee { get; set; }
}
