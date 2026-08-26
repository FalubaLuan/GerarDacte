namespace DacteNet.Rendering.Primitives;

/// <summary>
/// Unit conversion helpers. The ACBrCTe "Fortes Report Lite" .dfm layout uses one raw design unit
/// approx equal to 1/96 inch (see /home/claude/work/analysis/retrato_layout.md §1, "Conversion-factor
/// verification" - confirmed with high confidence against the 742-unit band width matching the A4
/// printable area). PDF points are 1/72 inch, so 1 raw RL unit = 0.75pt exactly.
/// </summary>
public static class Layout
{
    public const double RlUnitToPoint = 0.75;
    public const double MmToPoint = 72.0 / 25.4;

    public static double Pt(double rlUnits) => rlUnits * RlUnitToPoint;
    public static double PtFromMm(double mm) => mm * MmToPoint;
}

public enum TextAlign { Left, Center, Right }

[Flags]
public enum BorderSides
{
    None = 0,
    Left = 1,
    Top = 2,
    Right = 4,
    Bottom = 8,
    All = Left | Top | Right | Bottom
}

public readonly struct PdfColor
{
    public readonly double R, G, B;
    public PdfColor(double r, double g, double b) { R = r; G = g; B = b; }
    public static readonly PdfColor Black = new(0, 0, 0);
    public static readonly PdfColor White = new(1, 1, 1);
    public static readonly PdfColor Gray = new(0.5, 0.5, 0.5);
}
