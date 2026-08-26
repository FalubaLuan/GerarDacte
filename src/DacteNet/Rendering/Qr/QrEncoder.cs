using System.Text;

namespace DacteNet.Rendering.Qr;

public enum QrErrorCorrectionLevel { L, M, Q, H }

/// <summary>
/// A complete, from-scratch QR Code (ISO/IEC 18004) encoder using Byte mode (UTF-8) - sufficient
/// since this library only ever encodes URLs (the CT-e's infCTeSupl/qrCodCTe field). This is the
/// same public, standard algorithm implemented identically by every QR library (ZXing, Project
/// Nayuki's "QR Code generator library", qrencode, etc.) - specification data and algorithm, not
/// proprietary code.
/// </summary>
public static class QrEncoder
{
    /// <summary>
    /// Encodes text as a QR Code, automatically selecting the smallest version (1-40) that fits the
    /// data at the requested error-correction level and the best data mask (0-7) by the standard
    /// penalty-scoring rules. Returns the final N x N module matrix (true = dark), including all
    /// mandatory structural patterns, ready to render as-is. Does NOT include the mandatory quiet
    /// zone - that is the caller's responsibility.
    /// </summary>
    public static bool[,] Encode(string text, QrErrorCorrectionLevel level = QrErrorCorrectionLevel.M)
    {
        if (text is null) throw new ArgumentNullException(nameof(text));

        var bytes = Encoding.UTF8.GetBytes(text);
        int levelIndex = QrVersionTables.LevelIndex(level);

        int version = ChooseVersion(bytes.Length, levelIndex);
        int totalDataCodewords = DataCodewordCount(version, levelIndex);

        var dataCodewords = BuildDataCodewords(bytes, version, totalDataCodewords);
        var finalCodewords = InterleaveWithErrorCorrection(dataCodewords, version, levelIndex);

        int size = 17 + 4 * version;
        var modules = new bool[size, size];
        var isFunction = new bool[size, size];

        DrawTimingPatterns(modules, isFunction, size);
        DrawFinderPattern(modules, isFunction, size, 3, 3);
        DrawFinderPattern(modules, isFunction, size, size - 4, 3);
        DrawFinderPattern(modules, isFunction, size, 3, size - 4);
        DrawAlignmentPatterns(modules, isFunction, size, version);
        ReserveFormatInfoArea(isFunction, size);
        if (version >= 7) ReserveVersionInfoArea(isFunction, size);

        PlaceData(modules, isFunction, size, finalCodewords);

        int bestMask = ChooseBestMask(modules, isFunction, size, out var bestMatrix);

        WriteFormatInfo(bestMatrix, size, levelIndex, bestMask);
        if (version >= 7) WriteVersionInfo(bestMatrix, size, version);

        return bestMatrix;
    }

    // ------------------------------------------------------------------
    // Version selection & capacity
    // ------------------------------------------------------------------

    private static int CharCountBits(int version) => version <= 9 ? 8 : 16;

    private static int DataCodewordCount(int version, int levelIndex)
    {
        int total = QrVersionTables.TotalCodewords[version];
        int eccPerBlock = QrVersionTables.EccCodewordsPerBlock[levelIndex][version];
        int numBlocks = QrVersionTables.NumBlocks[levelIndex][version];
        return total - eccPerBlock * numBlocks;
    }

    private static int ChooseVersion(int byteLength, int levelIndex)
    {
        for (int version = 1; version <= 40; version++)
        {
            int capacityBits = DataCodewordCount(version, levelIndex) * 8;
            int requiredBits = 4 + CharCountBits(version) + byteLength * 8;
            if (requiredBits <= capacityBits) return version;
        }
        throw new ArgumentException("Text is too long to encode in a QR code even at version 40.", nameof(byteLength));
    }

    // ------------------------------------------------------------------
    // Bit-stream construction (byte mode) & padding
    // ------------------------------------------------------------------

    private sealed class BitWriter
    {
        public readonly List<bool> Bits = new();
        public void WriteBits(int value, int numBits)
        {
            for (int i = numBits - 1; i >= 0; i--)
                Bits.Add(((value >> i) & 1) != 0);
        }
    }

    private static byte[] BuildDataCodewords(byte[] data, int version, int totalDataCodewords)
    {
        var bw = new BitWriter();
        bw.WriteBits(0b0100, 4); // byte-mode indicator
        bw.WriteBits(data.Length, CharCountBits(version));
        foreach (var b in data) bw.WriteBits(b, 8);

        int capacityBits = totalDataCodewords * 8;
        int terminatorBits = Math.Min(4, capacityBits - bw.Bits.Count);
        if (terminatorBits > 0) bw.WriteBits(0, terminatorBits);

        // Pad with 0 bits to the next byte boundary.
        while (bw.Bits.Count % 8 != 0 && bw.Bits.Count < capacityBits)
            bw.Bits.Add(false);

        // Pad bytes 0xEC, 0x11 alternating until the codeword capacity is filled.
        bool useEc = true;
        while (bw.Bits.Count < capacityBits)
        {
            bw.WriteBits(useEc ? 0xEC : 0x11, 8);
            useEc = !useEc;
        }

        var codewords = new byte[totalDataCodewords];
        for (int i = 0; i < totalDataCodewords; i++)
        {
            int b = 0;
            for (int bit = 0; bit < 8; bit++)
                b = (b << 1) | (bw.Bits[i * 8 + bit] ? 1 : 0);
            codewords[i] = (byte)b;
        }
        return codewords;
    }

    // ------------------------------------------------------------------
    // Reed-Solomon block splitting, EC computation & interleaving
    // ------------------------------------------------------------------

    private static byte[] InterleaveWithErrorCorrection(byte[] dataCodewords, int version, int levelIndex)
    {
        int numBlocks = QrVersionTables.NumBlocks[levelIndex][version];
        int eccPerBlock = QrVersionTables.EccCodewordsPerBlock[levelIndex][version];

        int totalData = dataCodewords.Length;
        int shortBlockDataLen = totalData / numBlocks;
        int numLongBlocks = totalData % numBlocks;
        int numShortBlocks = numBlocks - numLongBlocks;

        var blockData = new byte[numBlocks][];
        var blockEcc = new byte[numBlocks][];
        int offset = 0;
        for (int i = 0; i < numBlocks; i++)
        {
            int len = i < numShortBlocks ? shortBlockDataLen : shortBlockDataLen + 1;
            blockData[i] = new byte[len];
            Array.Copy(dataCodewords, offset, blockData[i], 0, len);
            offset += len;
            blockEcc[i] = GaloisField.ComputeEccCodewords(blockData[i], eccPerBlock);
        }

        var result = new List<byte>(QrVersionTables.TotalCodewords[version]);
        int maxDataLen = shortBlockDataLen + 1;
        for (int col = 0; col < maxDataLen; col++)
        {
            for (int b = 0; b < numBlocks; b++)
            {
                if (col < blockData[b].Length) result.Add(blockData[b][col]);
            }
        }
        for (int col = 0; col < eccPerBlock; col++)
        {
            for (int b = 0; b < numBlocks; b++)
                result.Add(blockEcc[b][col]);
        }
        return result.ToArray();
    }

    // ------------------------------------------------------------------
    // Function pattern drawing
    // ------------------------------------------------------------------

    private static void DrawFinderPattern(bool[,] modules, bool[,] isFunction, int size, int centerX, int centerY)
    {
        for (int dy = -4; dy <= 4; dy++)
        {
            for (int dx = -4; dx <= 4; dx++)
            {
                int x = centerX + dx, y = centerY + dy;
                if (x < 0 || x >= size || y < 0 || y >= size) continue;
                int dist = Math.Max(Math.Abs(dx), Math.Abs(dy));
                bool dark = dist != 2 && dist != 4;
                modules[y, x] = dark;
                isFunction[y, x] = true;
            }
        }
    }

    private static void DrawAlignmentPattern(bool[,] modules, bool[,] isFunction, int size, int centerX, int centerY)
    {
        for (int dy = -2; dy <= 2; dy++)
        {
            for (int dx = -2; dx <= 2; dx++)
            {
                int x = centerX + dx, y = centerY + dy;
                bool dark = Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1;
                modules[y, x] = dark;
                isFunction[y, x] = true;
            }
        }
    }

    private static void DrawAlignmentPatterns(bool[,] modules, bool[,] isFunction, int size, int version)
    {
        var positions = QrVersionTables.AlignmentPatternPositions[version];
        if (positions.Length == 0) return;
        for (int i = 0; i < positions.Length; i++)
        {
            for (int j = 0; j < positions.Length; j++)
            {
                // Skip the three positions that coincide with the finder patterns.
                if ((i == 0 && j == 0) || (i == 0 && j == positions.Length - 1) || (i == positions.Length - 1 && j == 0))
                    continue;
                DrawAlignmentPattern(modules, isFunction, size, positions[j], positions[i]);
            }
        }
    }

    private static void DrawTimingPatterns(bool[,] modules, bool[,] isFunction, int size)
    {
        for (int i = 0; i < size; i++)
        {
            if (!isFunction[6, i]) { modules[6, i] = i % 2 == 0; isFunction[6, i] = true; }
            if (!isFunction[i, 6]) { modules[i, 6] = i % 2 == 0; isFunction[i, 6] = true; }
        }
    }

    private static void ReserveFormatInfoArea(bool[,] isFunction, int size)
    {
        for (int i = 0; i <= 5; i++) isFunction[i, 8] = true;
        isFunction[7, 8] = true;
        isFunction[8, 8] = true;
        isFunction[8, 7] = true;
        for (int i = 9; i < 15; i++) isFunction[8, 14 - i] = true;

        for (int i = 0; i <= 7; i++) isFunction[8, size - 1 - i] = true;
        for (int i = 8; i < 15; i++) isFunction[size - 15 + i, 8] = true;
    }

    private static void ReserveVersionInfoArea(bool[,] isFunction, int size)
    {
        for (int i = 0; i < 18; i++)
        {
            int a = size - 11 + i % 3;
            int b = i / 3;
            isFunction[b, a] = true;
            isFunction[a, b] = true;
        }
    }

    // ------------------------------------------------------------------
    // Data placement (zig-zag)
    // ------------------------------------------------------------------

    private static void PlaceData(bool[,] modules, bool[,] isFunction, int size, byte[] data)
    {
        int i = 0;
        int totalBits = data.Length * 8;
        for (int right = size - 1; right >= 1; right -= 2)
        {
            if (right == 6) right = 5;
            for (int vert = 0; vert < size; vert++)
            {
                for (int j = 0; j < 2; j++)
                {
                    int x = right - j;
                    bool upward = ((right + 1) & 2) == 0;
                    int y = upward ? size - 1 - vert : vert;
                    if (!isFunction[y, x] && i < totalBits)
                    {
                        bool bit = ((data[i >> 3] >> (7 - (i & 7))) & 1) != 0;
                        modules[y, x] = bit;
                        i++;
                    }
                }
            }
        }
    }

    // ------------------------------------------------------------------
    // Masking & penalty scoring
    // ------------------------------------------------------------------

    private static bool MaskBit(int maskPattern, int row, int col) => maskPattern switch
    {
        0 => (row + col) % 2 == 0,
        1 => row % 2 == 0,
        2 => col % 3 == 0,
        3 => (row + col) % 3 == 0,
        4 => (row / 2 + col / 3) % 2 == 0,
        5 => (row * col) % 2 + (row * col) % 3 == 0,
        6 => ((row * col) % 2 + (row * col) % 3) % 2 == 0,
        7 => ((row + col) % 2 + (row * col) % 3) % 2 == 0,
        _ => throw new ArgumentOutOfRangeException(nameof(maskPattern)),
    };

    private static int ChooseBestMask(bool[,] baseModules, bool[,] isFunction, int size, out bool[,] bestMatrix)
    {
        int bestMask = -1;
        int bestPenalty = int.MaxValue;
        bestMatrix = baseModules;

        for (int mask = 0; mask < 8; mask++)
        {
            var candidate = (bool[,])baseModules.Clone();
            for (int row = 0; row < size; row++)
            {
                for (int col = 0; col < size; col++)
                {
                    if (isFunction[row, col]) continue;
                    if (MaskBit(mask, row, col)) candidate[row, col] = !candidate[row, col];
                }
            }

            int penalty = ComputePenalty(candidate, size);
            if (penalty < bestPenalty)
            {
                bestPenalty = penalty;
                bestMask = mask;
                bestMatrix = candidate;
            }
        }
        return bestMask;
    }

    private static readonly bool[] FinderLikePatternA = { true, false, true, true, true, false, true, false, false, false, false };
    private static readonly bool[] FinderLikePatternB = { false, false, false, false, true, false, true, true, true, false, true };

    private static int ComputePenalty(bool[,] modules, int size)
    {
        int penalty = 0;

        for (int row = 0; row < size; row++)
        {
            var line = new bool[size];
            for (int col = 0; col < size; col++) line[col] = modules[row, col];
            penalty += RunPenalty(line);
            penalty += 40 * CountPatternOccurrences(line, FinderLikePatternA);
            penalty += 40 * CountPatternOccurrences(line, FinderLikePatternB);
        }
        for (int col = 0; col < size; col++)
        {
            var line = new bool[size];
            for (int row = 0; row < size; row++) line[row] = modules[row, col];
            penalty += RunPenalty(line);
            penalty += 40 * CountPatternOccurrences(line, FinderLikePatternA);
            penalty += 40 * CountPatternOccurrences(line, FinderLikePatternB);
        }

        for (int row = 0; row < size - 1; row++)
        {
            for (int col = 0; col < size - 1; col++)
            {
                bool c = modules[row, col];
                if (modules[row, col + 1] == c && modules[row + 1, col] == c && modules[row + 1, col + 1] == c)
                    penalty += 3;
            }
        }

        int dark = 0;
        for (int row = 0; row < size; row++)
            for (int col = 0; col < size; col++)
                if (modules[row, col]) dark++;
        int total = size * size;
        int k = Math.Abs(dark * 20 - total * 10) / total;
        penalty += k * 10;

        return penalty;
    }

    private static int RunPenalty(bool[] line)
    {
        int penalty = 0;
        int runLen = 1;
        for (int i = 1; i < line.Length; i++)
        {
            if (line[i] == line[i - 1])
            {
                runLen++;
            }
            else
            {
                if (runLen >= 5) penalty += 3 + (runLen - 5);
                runLen = 1;
            }
        }
        if (runLen >= 5) penalty += 3 + (runLen - 5);
        return penalty;
    }

    private static int CountPatternOccurrences(bool[] line, bool[] pattern)
    {
        int count = 0;
        for (int i = 0; i + pattern.Length <= line.Length; i++)
        {
            bool match = true;
            for (int k = 0; k < pattern.Length; k++)
            {
                if (line[i + k] != pattern[k]) { match = false; break; }
            }
            if (match) count++;
        }
        return count;
    }

    // ------------------------------------------------------------------
    // Format info (EC level + mask, BCH(15,5)) & version info (BCH(18,6))
    // ------------------------------------------------------------------

    /// <summary>Systematic BCH encode: returns (data &lt;&lt; degree) | remainder, per ISO/IEC 18004 Annex C/D.</summary>
    private static int BchEncode(int data, int dataBits, int generatorPoly, int degree)
    {
        int value = data << degree;
        for (int i = dataBits - 1; i >= 0; i--)
        {
            if ((value & (1 << (degree + i))) != 0)
                value ^= generatorPoly << i;
        }
        return (data << degree) | value;
    }

    private static void WriteFormatInfo(bool[,] modules, int size, int levelIndex, int maskPattern)
    {
        var level = levelIndex switch
        {
            0 => QrErrorCorrectionLevel.L,
            1 => QrErrorCorrectionLevel.M,
            2 => QrErrorCorrectionLevel.Q,
            _ => QrErrorCorrectionLevel.H,
        };
        int data5 = (QrVersionTables.FormatBits(level) << 3) | maskPattern;
        int bits = BchEncode(data5, 5, 0x537, 10) ^ 0x5412;

        bool Bit(int i) => ((bits >> i) & 1) != 0;

        for (int i = 0; i <= 5; i++) modules[i, 8] = Bit(i);
        modules[7, 8] = Bit(6);
        modules[8, 8] = Bit(7);
        modules[8, 7] = Bit(8);
        for (int i = 9; i < 15; i++) modules[8, 14 - i] = Bit(i);

        for (int i = 0; i <= 7; i++) modules[8, size - 1 - i] = Bit(i);
        for (int i = 8; i < 15; i++) modules[size - 15 + i, 8] = Bit(i);

        modules[size - 8, 8] = true; // fixed dark module
    }

    private static void WriteVersionInfo(bool[,] modules, int size, int version)
    {
        int bits = BchEncode(version, 6, 0x1F25, 12);
        bool Bit(int i) => ((bits >> i) & 1) != 0;

        for (int i = 0; i < 18; i++)
        {
            bool bit = Bit(i);
            int a = size - 11 + i % 3;
            int b = i / 3;
            modules[b, a] = bit;
            modules[a, b] = bit;
        }
    }
}
