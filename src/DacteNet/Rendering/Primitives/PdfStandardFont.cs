namespace DacteNet.Rendering.Primitives;

/// <summary>
/// One of the 14 standard PDF fonts. No embedding is required for these - every PDF viewer/printer
/// ships them - which is why this library has no font-file dependency at all. They are used to
/// approximate the three font families the original ACBr DACTE layout uses (see retrato_layout.md §1):
/// Times New Roman -&gt; Times-Roman/Bold, Arial -&gt; Helvetica/Bold, Courier New -&gt; Courier/Bold.
/// </summary>
public enum PdfStandardFont
{
    Helvetica,
    HelveticaBold,
    TimesRoman,
    TimesBold,
    Courier,
    CourierBold,
}

public static class PdfStandardFontExtensions
{
    public static string BaseFontName(this PdfStandardFont font) => font switch
    {
        PdfStandardFont.Helvetica => "Helvetica",
        PdfStandardFont.HelveticaBold => "Helvetica-Bold",
        PdfStandardFont.TimesRoman => "Times-Roman",
        PdfStandardFont.TimesBold => "Times-Bold",
        PdfStandardFont.Courier => "Courier",
        PdfStandardFont.CourierBold => "Courier-Bold",
        _ => throw new ArgumentOutOfRangeException(nameof(font)),
    };

    /// <summary>PDF resource name used inside content streams, e.g. "/FHelvetica".</summary>
    public static string ResourceName(this PdfStandardFont font) => "F" + (int)font;
}
