namespace DacteNet.Rendering.Primitives;

/// <summary>
/// Maps the three Delphi font families the original DACTE layout uses (retrato_layout.md §1: "Times New
/// Roman" for almost all body text, "Arial" for the canhoto date/time placeholders and the "barra" strip
/// canhoto, "Courier New" for a handful of memos) onto their closest standard-14 PDF equivalents.
/// </summary>
public readonly struct DacteFont
{
    public readonly PdfStandardFont Font;
    public readonly double SizePt;

    public DacteFont(PdfStandardFont font, double sizePt)
    {
        Font = font;
        SizePt = sizePt;
    }

    /// <summary>sizePt should be the already-converted point size (|Font.Height| * 0.75, per retrato_layout.md §1).</summary>
    public static DacteFont TimesNewRoman(double sizePt, bool bold = false) =>
        new(bold ? PdfStandardFont.TimesBold : PdfStandardFont.TimesRoman, sizePt);

    public static DacteFont Arial(double sizePt, bool bold = false) =>
        new(bold ? PdfStandardFont.HelveticaBold : PdfStandardFont.Helvetica, sizePt);

    public static DacteFont CourierNew(double sizePt, bool bold = false) =>
        new(bold ? PdfStandardFont.CourierBold : PdfStandardFont.Courier, sizePt);
}
