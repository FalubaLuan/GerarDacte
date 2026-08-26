using DacteNet.Rendering.Barcode;

namespace DacteNet.Rendering.Primitives;

/// <summary>
/// Draws using the exact same coordinate convention as the extracted ACBrCTe layout tables
/// (retrato_layout.md / retrato_a5_layout.md "L,T,W,H (raw)" columns): every geometry argument is a
/// raw Fortes-Report-Lite design unit (1 unit = 0.75pt, see <see cref="Layout"/>), relative to the
/// *current band's* own top-left origin - exactly like a Delphi child control's Left/Top is relative to
/// its parent band. This lets band-rendering code transcribe a table row almost verbatim, e.g. for
/// `rllNumCTe2` at raw (636,35,86,16): <c>canvas.Text(636, 35, 86, 16, numero, font)</c>.
///
/// Vertical stacking of bands (and page breaks) is handled by whichever renderer owns the canvas
/// (see Rendering/A4/DacteA4Renderer.cs) via <see cref="AdvanceBand"/>/<see cref="NewPage"/> - this class
/// only knows about "the current band's absolute page position", not about band order/visibility rules,
/// which are business/layout decisions that belong to the ViewModel and renderer respectively.
/// </summary>
public sealed class ReportCanvas
{
    private readonly PdfDocument _document;
    private readonly double _pageWidthPt;
    private readonly double _pageHeightPt;
    private readonly double _leftMarginPt;
    private readonly double _topMarginPt;
    private readonly double _bottomMarginPt;

    public PdfPage Page { get; private set; }

    /// <summary>Absolute distance from the top of the page (in points) where the current band's raw-unit Top=0 sits.</summary>
    public double BandTopPt { get; private set; }

    public double UsableWidthPt => _pageWidthPt - _leftMarginPt - _rightMarginPt;
    private readonly double _rightMarginPt;

    public ReportCanvas(PdfDocument document, double pageWidthPt, double pageHeightPt,
        double leftMarginPt, double topMarginPt, double rightMarginPt, double bottomMarginPt)
    {
        _document = document;
        _pageWidthPt = pageWidthPt;
        _pageHeightPt = pageHeightPt;
        _leftMarginPt = leftMarginPt;
        _topMarginPt = topMarginPt;
        _rightMarginPt = rightMarginPt;
        _bottomMarginPt = bottomMarginPt;
        Page = document.AddPage(pageWidthPt, pageHeightPt);
        BandTopPt = topMarginPt;
    }

    public double RemainingHeightPt => _pageHeightPt - _bottomMarginPt - BandTopPt;

    /// <summary>Starts a new physical page and resets the band cursor to the top margin.</summary>
    public void NewPage()
    {
        Page = _document.AddPage(_pageWidthPt, _pageHeightPt);
        BandTopPt = _topMarginPt;
    }

    /// <summary>Moves the band cursor down by the given band height (raw units) - call after finishing a band.</summary>
    public void AdvanceBand(double heightRl) => BandTopPt += Layout.Pt(heightRl);

    /// <summary>Explicitly sets the absolute band-cursor position (points from top of page) - for advanced pagination.</summary>
    public void SetBandTopPt(double topPt) => BandTopPt = topPt;

    private double AbsXPt(double leftRl) => _leftMarginPt + Layout.Pt(leftRl);
    private double AbsYTopDownPt(double topRl) => BandTopPt + Layout.Pt(topRl);
    private double ToPdfY(double topDownYPt) => _pageHeightPt - topDownYPt;

    /// <summary>
    /// Draws a single line of text inside the given band-relative box, top-anchored (like the original
    /// Delphi <c>TRLLabel</c>/<c>TRLMemo</c> controls this mirrors: their box height is typically just
    /// slightly taller than one text line, for visual padding, not meant to vertically center text in a
    /// much-taller box - a handful of DACTE fields use noticeably tall boxes, e.g. the 19-raw-unit-tall
    /// municipality box in rlb_03_DadosDACTe, and centering there visibly collided with the row below).
    /// </summary>
    public void Text(double leftRl, double topRl, double widthRl, double heightRl, string? text,
        DacteFont font, TextAlign align = TextAlign.Left, PdfColor? color = null)
    {
        if (string.IsNullOrEmpty(text)) return;
        var x = AbsXPt(leftRl);
        var w = Layout.Pt(widthRl);
        var boxTop = AbsYTopDownPt(topRl);

        var textWidth = AfmWidths.MeasureWidthPt(font.Font, text, font.SizePt);
        var textX = align switch
        {
            TextAlign.Center => x + (w - textWidth) / 2.0,
            TextAlign.Right => x + w - textWidth,
            _ => x,
        };

        // Top-anchored: nudge the baseline down by ~0.8em from the box's top edge, which is roughly
        // where a single line of text sits at the top of a Delphi label/memo control.
        var baselineTopDown = boxTop + font.SizePt * 0.8;
        Page.DrawText(textX, ToPdfY(baselineTopDown), text, font.Font, font.SizePt, color ?? PdfColor.Black);
    }

    /// <summary>Draws several lines of left-anchored text starting at the box's top, one under another (TRLMemo equivalent).</summary>
    public void Memo(double leftRl, double topRl, double widthRl, IEnumerable<string> lines, DacteFont font,
        TextAlign align = TextAlign.Left, PdfColor? color = null, double lineHeightRl = 12)
    {
        double t = topRl;
        foreach (var line in lines)
        {
            Text(leftRl, t, widthRl, lineHeightRl, line, font, align, color);
            t += lineHeightRl;
        }
    }

    public void Line(double x1Rl, double y1Rl, double x2Rl, double y2Rl, double lineWidthPt = 0.75, PdfColor? color = null)
    {
        Page.StrokeLine(AbsXPt(x1Rl), ToPdfY(AbsYTopDownPt(y1Rl)), AbsXPt(x2Rl), ToPdfY(AbsYTopDownPt(y2Rl)),
            lineWidthPt, color ?? PdfColor.Black);
    }

    public void Rect(double leftRl, double topRl, double widthRl, double heightRl,
        BorderSides sides = BorderSides.All, double lineWidthPt = 0.75, PdfColor? color = null)
    {
        var x = AbsXPt(leftRl);
        var boxTop = AbsYTopDownPt(topRl);
        var w = Layout.Pt(widthRl);
        var h = Layout.Pt(heightRl);
        // PDF rectangles are specified by their bottom-left corner; our box's bottom (in top-down terms) is boxTop+h.
        Page.StrokeRectSides(x, ToPdfY(boxTop + h), w, h, lineWidthPt, color ?? PdfColor.Black, sides);
    }

    public void FilledRect(double leftRl, double topRl, double widthRl, double heightRl, PdfColor color)
    {
        var x = AbsXPt(leftRl);
        var boxTop = AbsYTopDownPt(topRl);
        var w = Layout.Pt(widthRl);
        var h = Layout.Pt(heightRl);
        Page.FillRect(x, ToPdfY(boxTop + h), w, h, color);
    }

    public void Image(PdfImage image, double leftRl, double topRl, double widthRl, double heightRl)
    {
        var x = AbsXPt(leftRl);
        var boxTop = AbsYTopDownPt(topRl);
        var w = Layout.Pt(widthRl);
        var h = Layout.Pt(heightRl);
        Page.DrawImage(image, x, ToPdfY(boxTop + h), w, h);
    }

    /// <summary>Renders a Code128 barcode (Code Set C, i.e. two decimal digits per symbol - the digitsOnly string must have even length) into the given box.</summary>
    public void Barcode128(double leftRl, double topRl, double widthRl, double heightRl, string digitsOnly)
    {
        if (string.IsNullOrEmpty(digitsOnly)) return;
        var modules = Code128Encoder.EncodeModules(digitsOnly);
        var x = AbsXPt(leftRl);
        var boxTop = AbsYTopDownPt(topRl);
        var w = Layout.Pt(widthRl);
        var h = Layout.Pt(heightRl);
        var y = ToPdfY(boxTop + h);

        int totalModules = modules.Sum();
        if (totalModules == 0) return;
        double moduleWidth = w / totalModules;

        double cursor = x;
        bool isBar = true;
        foreach (var moduleCount in modules)
        {
            var segWidth = moduleCount * moduleWidth;
            if (isBar) Page.FillRect(cursor, y, segWidth, h, PdfColor.Black);
            cursor += segWidth;
            isBar = !isBar;
        }
    }

    /// <summary>Renders a QR Code (drawn as vector filled squares - no raster image involved) into the given box.</summary>
    public void QrCode(double leftRl, double topRl, double sizeRl, string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        var modules = DacteNet.Rendering.Qr.QrEncoder.Encode(text);
        int n = modules.GetLength(0);
        var x = AbsXPt(leftRl);
        var boxTop = AbsYTopDownPt(topRl);
        var size = Layout.Pt(sizeRl);
        var moduleSize = size / n;

        for (int row = 0; row < n; row++)
        {
            for (int col = 0; col < n; col++)
            {
                if (!modules[row, col]) continue;
                var moduleTopDown = boxTop + row * moduleSize;
                Page.FillRect(x + col * moduleSize, ToPdfY(moduleTopDown + moduleSize), moduleSize, moduleSize, PdfColor.Black);
            }
        }
    }
}
