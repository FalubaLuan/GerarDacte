using System.Globalization;
using System.Text;

namespace DacteNet.Rendering.Primitives;

/// <summary>
/// One PDF page. All drawing methods use true PDF coordinate space: origin at the bottom-left corner,
/// y increasing upward, units in points (1/72 inch). Higher-level code (<see cref="ReportCanvas"/>)
/// is responsible for converting the DACTE layout's top-down, band-relative coordinates into this space.
/// </summary>
public sealed class PdfPage
{
    public double WidthPt { get; }
    public double HeightPt { get; }

    private readonly StringBuilder _content = new();
    internal readonly HashSet<PdfStandardFont> UsedFonts = new();
    internal readonly List<PdfImage> Images = new();
    private readonly Dictionary<PdfImage, string> _imageResourceNames = new();

    public PdfPage(double widthPt, double heightPt)
    {
        WidthPt = widthPt;
        HeightPt = heightPt;
    }

    private static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);

    public void StrokeLine(double x1, double y1, double x2, double y2, double lineWidthPt, PdfColor color)
    {
        _content.Append(F(color.R)).Append(' ').Append(F(color.G)).Append(' ').Append(F(color.B)).Append(" RG\n");
        _content.Append(F(lineWidthPt)).Append(" w\n");
        _content.Append(F(x1)).Append(' ').Append(F(y1)).Append(" m ")
                .Append(F(x2)).Append(' ').Append(F(y2)).Append(" l S\n");
    }

    /// <summary>Strokes only the requested sides of a rectangle whose bottom-left corner is (x,y).</summary>
    public void StrokeRectSides(double x, double y, double w, double h, double lineWidthPt, PdfColor color, BorderSides sides)
    {
        if (sides == BorderSides.None) return;
        if (sides == BorderSides.All)
        {
            _content.Append(F(color.R)).Append(' ').Append(F(color.G)).Append(' ').Append(F(color.B)).Append(" RG\n");
            _content.Append(F(lineWidthPt)).Append(" w\n");
            _content.Append(F(x)).Append(' ').Append(F(y)).Append(' ').Append(F(w)).Append(' ').Append(F(h)).Append(" re S\n");
            return;
        }
        if ((sides & BorderSides.Bottom) != 0) StrokeLine(x, y, x + w, y, lineWidthPt, color);
        if ((sides & BorderSides.Top) != 0) StrokeLine(x, y + h, x + w, y + h, lineWidthPt, color);
        if ((sides & BorderSides.Left) != 0) StrokeLine(x, y, x, y + h, lineWidthPt, color);
        if ((sides & BorderSides.Right) != 0) StrokeLine(x + w, y, x + w, y + h, lineWidthPt, color);
    }

    public void FillRect(double x, double y, double w, double h, PdfColor color)
    {
        _content.Append(F(color.R)).Append(' ').Append(F(color.G)).Append(' ').Append(F(color.B)).Append(" rg\n");
        _content.Append(F(x)).Append(' ').Append(F(y)).Append(' ').Append(F(w)).Append(' ').Append(F(h)).Append(" re f\n");
    }

    /// <summary>Draws a single line of left-anchored text with its baseline at (x,y).</summary>
    public void DrawText(double x, double y, string text, PdfStandardFont font, double sizePt, PdfColor color)
    {
        if (string.IsNullOrEmpty(text)) return;
        UsedFonts.Add(font);
        var bytes = WinAnsiEncoding.ToPdfLiteralBytes(text);

        _content.Append(F(color.R)).Append(' ').Append(F(color.G)).Append(' ').Append(F(color.B)).Append(" rg\n");
        _content.Append("BT /").Append(font.ResourceName()).Append(' ').Append(F(sizePt)).Append(" Tf ")
                .Append(F(x)).Append(' ').Append(F(y)).Append(" Td (");
        AppendRawBytesAsLatin1(_content, bytes);
        _content.Append(") Tj ET\n");
    }

    // The content stream is emitted as a Latin-1 string and re-encoded to raw bytes at Save() time
    // (see PdfDocument.Save), so every byte 0x00-0xFF we append here must round-trip through Latin-1
    // untouched - Latin-1 maps code point N to byte N for the full 0-255 range, which is exactly what we need.
    private static void AppendRawBytesAsLatin1(StringBuilder sb, byte[] bytes)
    {
        foreach (var b in bytes) sb.Append((char)b);
    }

    /// <summary>Draws a JPEG image (DCTDecode passthrough - see <see cref="PdfImage"/>) into the given box, bottom-left corner (x,y).</summary>
    public void DrawImage(PdfImage image, double x, double y, double w, double h)
    {
        if (!Images.Contains(image)) Images.Add(image);
        _content.Append("q ").Append(F(w)).Append(" 0 0 ").Append(F(h)).Append(' ').Append(F(x)).Append(' ').Append(F(y)).Append(" cm /")
                .Append(ImageResourceName(image)).Append(" Do Q\n");
    }

    internal string ImageResourceName(PdfImage image)
    {
        if (!_imageResourceNames.TryGetValue(image, out var name))
        {
            name = "Im" + _imageResourceNames.Count;
            _imageResourceNames[image] = name;
        }
        return name;
    }

    internal string GetContentStream() => _content.ToString();
}
