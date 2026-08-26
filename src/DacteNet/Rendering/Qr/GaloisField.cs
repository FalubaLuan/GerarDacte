namespace DacteNet.Rendering.Qr;

/// <summary>
/// GF(256) arithmetic (primitive polynomial x^8 + x^4 + x^3 + x^2 + 1 = 0x11D) and Reed-Solomon
/// error-correction codeword generation, exactly as specified by ISO/IEC 18004 Annex A. This is
/// the same standard construction used by every QR Code implementation (ZXing, Project Nayuki's
/// "QR Code generator library", qrencode, etc.) - public specification algorithm, not proprietary
/// code.
/// </summary>
internal static class GaloisField
{
    private const int PrimitivePolynomial = 0x11D;
    private static readonly byte[] ExpTable = new byte[256];
    private static readonly byte[] LogTable = new byte[256];

    static GaloisField()
    {
        int x = 1;
        for (int i = 0; i < 255; i++)
        {
            ExpTable[i] = (byte)x;
            LogTable[x] = (byte)i;
            x <<= 1;
            if ((x & 0x100) != 0) x ^= PrimitivePolynomial;
        }
        // ExpTable[255] would wrap back to ExpTable[0] = 1; leave it as the default 0 slot is never
        // indexed because log values are always in [0,254] for nonzero elements.
    }

    private static byte Multiply(byte a, byte b)
    {
        if (a == 0 || b == 0) return 0;
        int logSum = LogTable[a] + LogTable[b];
        if (logSum >= 255) logSum -= 255;
        return ExpTable[logSum];
    }

    /// <summary>
    /// Builds the degree-N Reed-Solomon generator polynomial coefficients (monic, highest-degree
    /// coefficient implicit as 1 and not stored - this returns the remaining N coefficients, matching
    /// the standard "compute divisor" construction: product over i=0..N-1 of (x - alpha^i)).
    /// </summary>
    private static byte[] ComputeGeneratorPolynomial(int degree)
    {
        var coeffs = new byte[degree];
        coeffs[degree - 1] = 1;
        byte root = 1;
        for (int i = 0; i < degree; i++)
        {
            for (int j = 0; j < degree; j++)
            {
                coeffs[j] = Multiply(coeffs[j], root);
                if (j + 1 < degree) coeffs[j] ^= coeffs[j + 1];
            }
            root = Multiply(root, 2);
        }
        return coeffs;
    }

    /// <summary>
    /// Computes the <paramref name="eccCount"/> Reed-Solomon error-correction codewords for one block
    /// of data codewords (polynomial long division of the data (shifted by eccCount bytes) modulo the
    /// generator polynomial, all arithmetic in GF(256)).
    /// </summary>
    public static byte[] ComputeEccCodewords(byte[] data, int eccCount)
    {
        var generator = ComputeGeneratorPolynomial(eccCount);
        var result = new byte[eccCount];
        foreach (var b in data)
        {
            byte factor = (byte)(b ^ result[0]);
            Array.Copy(result, 1, result, 0, eccCount - 1);
            result[eccCount - 1] = 0;
            for (int i = 0; i < eccCount; i++)
                result[i] ^= Multiply(generator[i], factor);
        }
        return result;
    }
}
