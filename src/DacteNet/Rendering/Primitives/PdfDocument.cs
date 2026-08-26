using System.Globalization;
using System.Text;

namespace DacteNet.Rendering.Primitives;

/// <summary>
/// A minimal, dependency-free PDF writer: just enough of the PDF 1.4 object model (catalog, page tree,
/// pages, uncompressed content streams, standard-14 fonts, DCTDecode image XObjects) to render the
/// DACTE. See docs/README.md "Why a hand-written PDF writer" for the rationale.
/// </summary>
public sealed class PdfDocument
{
    private readonly List<PdfPage> _pages = new();

    public PdfPage AddPage(double widthPt, double heightPt)
    {
        var page = new PdfPage(widthPt, heightPt);
        _pages.Add(page);
        return page;
    }

    public IReadOnlyList<PdfPage> Pages => _pages;

    public void Save(Stream output)
    {
        var objects = new List<byte[]>(); // index 0 unused (object numbers start at 1)
        var isPreWrapped = new List<bool>(); // true when the stored bytes already include "N 0 obj ... endobj"
        objects.Add(Array.Empty<byte>());
        isPreWrapped.Add(false);

        int Alloc() { objects.Add(Array.Empty<byte>()); isPreWrapped.Add(false); return objects.Count - 1; }
        void Set(int id, string body) => objects[id] = Encoding.Latin1.GetBytes(body);
        void SetBytes(int id, byte[] body) { objects[id] = body; isPreWrapped[id] = true; }

        // Fonts actually used anywhere in the document.
        var usedFonts = new SortedSet<PdfStandardFont>(_pages.SelectMany(p => p.UsedFonts).Select(f => f).Distinct().OrderBy(f => (int)f));
        var fontObjIds = new Dictionary<PdfStandardFont, int>();
        foreach (var font in usedFonts)
        {
            int id = Alloc();
            fontObjIds[font] = id;
            Set(id, $"<< /Type /Font /Subtype /Type1 /BaseFont /{font.BaseFontName()} /Encoding /WinAnsiEncoding >>");
        }

        // Images actually used anywhere in the document.
        var allImages = _pages.SelectMany(p => p.Images).Distinct().ToList();
        var imageObjIds = new Dictionary<PdfImage, int>();
        foreach (var img in allImages)
        {
            int id = Alloc();
            imageObjIds[img] = id;
            var colorSpace = img.Components switch { 1 => "DeviceGray", 4 => "DeviceCMYK", _ => "DeviceRGB" };
            var header =
                $"<< /Type /XObject /Subtype /Image /Width {img.PixelWidth} /Height {img.PixelHeight} " +
                $"/ColorSpace /{colorSpace} /BitsPerComponent 8 /Filter /DCTDecode /Length {img.JpegBytes.Length} >>\nstream\n";
            var headerBytes = Encoding.Latin1.GetBytes(header);
            var footerBytes = Encoding.Latin1.GetBytes("\nendstream");
            var combined = new byte[headerBytes.Length + img.JpegBytes.Length + footerBytes.Length];
            Buffer.BlockCopy(headerBytes, 0, combined, 0, headerBytes.Length);
            Buffer.BlockCopy(img.JpegBytes, 0, combined, headerBytes.Length, img.JpegBytes.Length);
            Buffer.BlockCopy(footerBytes, 0, combined, headerBytes.Length + img.JpegBytes.Length, footerBytes.Length);
            SetBytes(id, WrapObj(id, combined));
        }

        int pagesTreeId = Alloc();
        var pageIds = new List<int>();

        foreach (var page in _pages)
        {
            int contentId = Alloc();
            var contentBytes = Encoding.Latin1.GetBytes(page.GetContentStream());
            var streamHeader = Encoding.Latin1.GetBytes($"<< /Length {contentBytes.Length} >>\nstream\n");
            var streamFooter = Encoding.Latin1.GetBytes("\nendstream");
            var combined = new byte[streamHeader.Length + contentBytes.Length + streamFooter.Length];
            Buffer.BlockCopy(streamHeader, 0, combined, 0, streamHeader.Length);
            Buffer.BlockCopy(contentBytes, 0, combined, streamHeader.Length, contentBytes.Length);
            Buffer.BlockCopy(streamFooter, 0, combined, streamHeader.Length + contentBytes.Length, streamFooter.Length);
            SetBytes(contentId, WrapObj(contentId, combined));

            int pageId = Alloc();
            pageIds.Add(pageId);

            var fontResEntries = string.Join(" ", page.UsedFonts.Select(f => $"/{f.ResourceName()} {fontObjIds[f]} 0 R"));
            var imageResEntries = string.Join(" ", page.Images.Select(im => $"/{page.ImageResourceName(im)} {imageObjIds[im]} 0 R"));
            var resources = "<< " +
                             (fontResEntries.Length > 0 ? $"/Font << {fontResEntries} >> " : "") +
                             (imageResEntries.Length > 0 ? $"/XObject << {imageResEntries} >> " : "") +
                             ">>";

            Set(pageId,
                $"<< /Type /Page /Parent {pagesTreeId} 0 R " +
                $"/MediaBox [0 0 {F(page.WidthPt)} {F(page.HeightPt)}] " +
                $"/Resources {resources} /Contents {contentId} 0 R >>");
        }

        Set(pagesTreeId,
            $"<< /Type /Pages /Kids [{string.Join(" ", pageIds.Select(id => $"{id} 0 R"))}] /Count {pageIds.Count} >>");

        int catalogId = Alloc();
        Set(catalogId, $"<< /Type /Catalog /Pages {pagesTreeId} 0 R >>");

        // --- Write out: header, each object (recording byte offsets), xref table, trailer ---
        using var buffer = new MemoryStream();
        void Write(string s) => buffer.Write(Encoding.Latin1.GetBytes(s));

        Write("%PDF-1.4\n%âãÏÓ\n");

        var offsets = new long[objects.Count];
        for (int id = 1; id < objects.Count; id++)
        {
            offsets[id] = buffer.Position;
            var body = objects[id];
            if (isPreWrapped[id])
            {
                buffer.Write(body);
            }
            else
            {
                Write($"{id} 0 obj\n{Encoding.Latin1.GetString(body)}\nendobj\n");
            }
        }

        long xrefOffset = buffer.Position;
        Write($"xref\n0 {objects.Count}\n0000000000 65535 f \n");
        for (int id = 1; id < objects.Count; id++)
            Write($"{offsets[id]:D10} 00000 n \n");

        Write($"trailer\n<< /Size {objects.Count} /Root {catalogId} 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");

        buffer.Position = 0;
        buffer.CopyTo(output);
    }

    public byte[] ToBytes()
    {
        using var ms = new MemoryStream();
        Save(ms);
        return ms.ToArray();
    }

    private static byte[] WrapObj(int id, byte[] body)
    {
        var header = Encoding.Latin1.GetBytes($"{id} 0 obj\n");
        var footer = Encoding.Latin1.GetBytes("\nendobj\n");
        var combined = new byte[header.Length + body.Length + footer.Length];
        Buffer.BlockCopy(header, 0, combined, 0, header.Length);
        Buffer.BlockCopy(body, 0, combined, header.Length, body.Length);
        Buffer.BlockCopy(footer, 0, combined, header.Length + body.Length, footer.Length);
        return combined;
    }

    private static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
}
