namespace DacteNet.Rendering.Primitives;

/// <summary>
/// A JPEG image ready to be embedded as a PDF XObject via direct DCTDecode passthrough - i.e. the
/// original JPEG byte stream is copied into the PDF unchanged (no re-encoding, no dependency on an
/// imaging library). Width/height/component-count are read directly from the JPEG's own SOF marker.
/// PNG or other formats are intentionally not supported without an extra conversion step by the
/// caller (see docs/limitations.md) - the issuer logo is a print-configuration concern, not CT-e data,
/// so this is a deliberately narrow feature.
/// </summary>
public sealed class PdfImage
{
    public byte[] JpegBytes { get; }
    public int PixelWidth { get; }
    public int PixelHeight { get; }
    public int Components { get; }

    private PdfImage(byte[] jpegBytes, int width, int height, int components)
    {
        JpegBytes = jpegBytes;
        PixelWidth = width;
        PixelHeight = height;
        Components = components;
    }

    /// <summary>Parses a raw JPEG byte stream (as produced by any standard encoder) for embedding.</summary>
    public static PdfImage FromJpegBytes(byte[] jpegBytes)
    {
        if (jpegBytes.Length < 4 || jpegBytes[0] != 0xFF || jpegBytes[1] != 0xD8)
            throw new ArgumentException("Os dados informados não são um JPEG válido (assinatura SOI ausente).");

        int i = 2;
        while (i + 4 <= jpegBytes.Length)
        {
            if (jpegBytes[i] != 0xFF) { i++; continue; }
            byte marker = jpegBytes[i + 1];
            if (marker == 0xD8 || marker == 0xD9) { i += 2; continue; } // SOI/EOI, no length field
            if (marker >= 0xD0 && marker <= 0xD7) { i += 2; continue; } // RSTn, no length field

            int segmentLength = (jpegBytes[i + 2] << 8) | jpegBytes[i + 3];

            bool isSof = marker is 0xC0 or 0xC1 or 0xC2 or 0xC3 or 0xC5 or 0xC6 or 0xC7
                or 0xC9 or 0xCA or 0xCB or 0xCD or 0xCE or 0xCF;
            if (isSof)
            {
                int height = (jpegBytes[i + 5] << 8) | jpegBytes[i + 6];
                int width = (jpegBytes[i + 7] << 8) | jpegBytes[i + 8];
                int components = jpegBytes[i + 9];
                return new PdfImage(jpegBytes, width, height, components);
            }

            i += 2 + segmentLength;
        }

        throw new ArgumentException("Não foi possível localizar as dimensões do JPEG (marcador SOF ausente).");
    }
}
