using System.Text;

namespace DacteNet.Rendering.Primitives;

/// <summary>
/// Minimal Windows-1252 ("WinAnsiEncoding" in PDF terms) text encoder, written by hand so the library
/// has no dependency on the System.Text.Encoding.CodePages package. Code points 0x00-0x7F are plain
/// ASCII; 0xA0-0xFF are numerically identical between Unicode and cp1252 (both equal Latin-1 there), which
/// covers every accented character CT-e/DACTE text needs (À-ÿ). Only the 0x80-0x9F block differs from
/// Unicode and is covered by <see cref="SpecialsMap"/> (smart quotes, dashes, €, etc.) purely so those
/// characters degrade gracefully instead of throwing; CT-e content essentially never uses them.
/// </summary>
public static class WinAnsiEncoding
{
    private static readonly Dictionary<char, byte> SpecialsMap = new()
    {
        ['€'] = 0x80, // €
        ['‚'] = 0x82,
        ['ƒ'] = 0x83,
        ['„'] = 0x84,
        ['…'] = 0x85, // …
        ['†'] = 0x86,
        ['‡'] = 0x87,
        ['ˆ'] = 0x88,
        ['‰'] = 0x89,
        ['Š'] = 0x8A,
        ['‹'] = 0x8B,
        ['Œ'] = 0x8C,
        ['Ž'] = 0x8E,
        ['‘'] = 0x91, // '
        ['’'] = 0x92, // '
        ['“'] = 0x93, // "
        ['”'] = 0x94, // "
        ['•'] = 0x95, // bullet
        ['–'] = 0x96, // en dash
        ['—'] = 0x97, // em dash
        ['˜'] = 0x98,
        ['™'] = 0x99, // TM
        ['š'] = 0x9A,
        ['›'] = 0x9B,
        ['œ'] = 0x9C,
        ['ž'] = 0x9E,
        ['Ÿ'] = 0x9F,
    };

    public static byte ToByte(char c)
    {
        if (c <= 0xFF) return (byte)c;
        return SpecialsMap.TryGetValue(c, out var b) ? b : (byte)'?';
    }

    public static byte[] GetBytes(string s)
    {
        var bytes = new byte[s.Length];
        for (int i = 0; i < s.Length; i++)
            bytes[i] = ToByte(s[i]);
        return bytes;
    }

    /// <summary>Escapes a string for use inside a PDF literal string "( ... )" token, encoding to WinAnsi bytes first.</summary>
    public static byte[] ToPdfLiteralBytes(string s)
    {
        var raw = GetBytes(s);
        var ms = new MemoryStream();
        foreach (var b in raw)
        {
            if (b is (byte)'(' or (byte)')' or (byte)'\\')
                ms.WriteByte((byte)'\\');
            ms.WriteByte(b);
        }
        return ms.ToArray();
    }
}
