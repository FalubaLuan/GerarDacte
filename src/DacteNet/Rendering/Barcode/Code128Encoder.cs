namespace DacteNet.Rendering.Barcode;

/// <summary>
/// Encodes numeric strings as Code 128 (ISO/IEC 15417) using Code Set C - two decimal digits per
/// symbol - which is the standard, most compact choice for all-numeric data such as the CT-e/NF-e
/// 44-digit access key (this matches ACBr's own bcCode128C barcode type used on the printed DACTE).
/// </summary>
public static class Code128Encoder
{
    private const int StartC = 105;
    private const int Stop = 106;

    /// <summary>
    /// The full Code 128 symbol-to-bar-pattern table (values 0-106), each entry giving the widths (in
    /// modules, 1-4) of six alternating bar/space/bar/space/bar/space elements - except entry 106
    /// (STOP), which has a seventh element: the mandatory trailing termination bar. This is the
    /// standard, public ISO/IEC 15417 pattern table reproduced identically by every Code 128
    /// implementation (e.g. ZXing's Code128Reader.CODE_PATTERNS).
    /// </summary>
    private static readonly int[][] Patterns =
    {
        new[] {2,1,2,2,2,2}, // 0
        new[] {2,2,2,1,2,2}, // 1
        new[] {2,2,2,2,2,1}, // 2
        new[] {1,2,1,2,2,3}, // 3
        new[] {1,2,1,3,2,2}, // 4
        new[] {1,3,1,2,2,2}, // 5
        new[] {1,2,2,2,1,3}, // 6
        new[] {1,2,2,3,1,2}, // 7
        new[] {1,3,2,2,1,2}, // 8
        new[] {2,2,1,2,1,3}, // 9
        new[] {2,2,1,3,1,2}, // 10
        new[] {2,3,1,2,1,2}, // 11
        new[] {1,1,2,2,3,2}, // 12
        new[] {1,2,2,1,3,2}, // 13
        new[] {1,2,2,2,3,1}, // 14
        new[] {1,1,3,2,2,2}, // 15
        new[] {1,2,3,1,2,2}, // 16
        new[] {1,2,3,2,2,1}, // 17
        new[] {2,2,3,2,1,1}, // 18
        new[] {2,2,1,1,3,2}, // 19
        new[] {2,2,1,2,3,1}, // 20
        new[] {2,1,3,2,1,2}, // 21
        new[] {2,2,3,1,1,2}, // 22
        new[] {3,1,2,1,3,1}, // 23
        new[] {3,1,1,2,2,2}, // 24
        new[] {3,2,1,1,2,2}, // 25
        new[] {3,2,1,2,2,1}, // 26
        new[] {3,1,2,2,1,2}, // 27
        new[] {3,2,2,1,1,2}, // 28
        new[] {3,2,2,2,1,1}, // 29
        new[] {2,1,2,1,2,3}, // 30
        new[] {2,1,2,3,2,1}, // 31
        new[] {2,3,2,1,2,1}, // 32
        new[] {1,1,1,3,2,3}, // 33
        new[] {1,3,1,1,2,3}, // 34
        new[] {1,3,1,3,2,1}, // 35
        new[] {1,1,2,3,1,3}, // 36
        new[] {1,3,2,1,1,3}, // 37
        new[] {1,3,2,3,1,1}, // 38
        new[] {2,1,1,3,1,3}, // 39
        new[] {2,3,1,1,1,3}, // 40
        new[] {2,3,1,3,1,1}, // 41
        new[] {1,1,2,1,3,3}, // 42
        new[] {1,1,2,3,3,1}, // 43
        new[] {1,3,2,1,3,1}, // 44
        new[] {1,1,3,1,2,3}, // 45
        new[] {1,1,3,3,2,1}, // 46
        new[] {1,3,3,1,2,1}, // 47
        new[] {3,1,3,1,2,1}, // 48
        new[] {2,1,1,3,3,1}, // 49
        new[] {2,3,1,1,3,1}, // 50
        new[] {2,1,3,1,1,3}, // 51
        new[] {2,1,3,3,1,1}, // 52
        new[] {2,1,3,1,3,1}, // 53
        new[] {3,1,1,1,2,3}, // 54
        new[] {3,1,1,3,2,1}, // 55
        new[] {3,3,1,1,2,1}, // 56
        new[] {3,1,2,1,1,3}, // 57
        new[] {3,1,2,3,1,1}, // 58
        new[] {3,3,2,1,1,1}, // 59
        new[] {3,1,4,1,1,1}, // 60
        new[] {2,2,1,4,1,1}, // 61
        new[] {4,3,1,1,1,1}, // 62
        new[] {1,1,1,2,2,4}, // 63
        new[] {1,1,1,4,2,2}, // 64
        new[] {1,2,1,1,2,4}, // 65
        new[] {1,2,1,4,2,1}, // 66
        new[] {1,4,1,1,2,2}, // 67
        new[] {1,4,1,2,2,1}, // 68
        new[] {1,1,2,2,1,4}, // 69
        new[] {1,1,2,4,1,2}, // 70
        new[] {1,2,2,1,1,4}, // 71
        new[] {1,2,2,4,1,1}, // 72
        new[] {1,4,2,1,1,2}, // 73
        new[] {1,4,2,2,1,1}, // 74
        new[] {2,4,1,2,1,1}, // 75
        new[] {2,2,1,1,1,4}, // 76
        new[] {4,1,3,1,1,1}, // 77
        new[] {2,4,1,1,1,2}, // 78
        new[] {1,3,4,1,1,1}, // 79
        new[] {1,1,1,2,4,2}, // 80
        new[] {1,2,1,1,4,2}, // 81
        new[] {1,2,1,2,4,1}, // 82
        new[] {1,1,4,2,1,2}, // 83
        new[] {1,2,4,1,1,2}, // 84
        new[] {1,2,4,2,1,1}, // 85
        new[] {4,1,1,2,1,2}, // 86
        new[] {4,2,1,1,1,2}, // 87
        new[] {4,2,1,2,1,1}, // 88
        new[] {2,1,2,1,4,1}, // 89
        new[] {2,1,4,1,2,1}, // 90
        new[] {4,1,2,1,2,1}, // 91
        new[] {1,1,1,1,4,3}, // 92
        new[] {1,1,1,3,4,1}, // 93
        new[] {1,3,1,1,4,1}, // 94
        new[] {1,1,4,1,1,3}, // 95
        new[] {1,1,4,3,1,1}, // 96
        new[] {4,1,1,1,1,3}, // 97
        new[] {4,1,1,3,1,1}, // 98
        new[] {1,1,3,1,4,1}, // 99
        new[] {1,1,4,1,3,1}, // 100
        new[] {3,1,1,1,4,1}, // 101
        new[] {4,1,1,1,3,1}, // 102
        new[] {2,1,1,4,1,2}, // 103 START A
        new[] {2,1,1,2,1,4}, // 104 START B
        new[] {2,1,1,2,3,2}, // 105 START C
        new[] {2,3,3,1,1,1,2}, // 106 STOP (includes trailing 2-module termination bar)
    };

    /// <summary>
    /// Encodes an all-numeric string using Code 128 Code Set C. Returns the module-width pattern as a
    /// flat sequence of run lengths (in modules), starting with a black bar and alternating
    /// black/white thereafter, including the start pattern, checksum symbol, and stop pattern (with
    /// its trailing termination bar). Quiet-zone whitespace margins are the caller's responsibility.
    /// </summary>
    public static int[] EncodeModules(string digitsOnly)
    {
        if (string.IsNullOrEmpty(digitsOnly))
            throw new ArgumentException("Input must not be null or empty.", nameof(digitsOnly));
        if (digitsOnly.Length % 2 != 0)
            throw new ArgumentException("Input must have an even number of digits for Code Set C.", nameof(digitsOnly));
        foreach (var ch in digitsOnly)
        {
            if (ch < '0' || ch > '9')
                throw new ArgumentException($"Input must contain only decimal digits; found '{ch}'.", nameof(digitsOnly));
        }

        var symbolValues = new List<int> { StartC };
        for (int i = 0; i < digitsOnly.Length; i += 2)
        {
            int tens = digitsOnly[i] - '0';
            int units = digitsOnly[i + 1] - '0';
            symbolValues.Add(tens * 10 + units);
        }

        // Modulo-103 checksum: start symbol counts as position 1 (weight 1), each subsequent
        // codeword's weight increases by 1.
        int checksum = 0;
        for (int pos = 0; pos < symbolValues.Count; pos++)
        {
            int weight = pos == 0 ? 1 : pos;
            checksum += symbolValues[pos] * weight;
        }
        checksum %= 103;

        symbolValues.Add(checksum);
        symbolValues.Add(Stop);

        var modules = new List<int>();
        foreach (var value in symbolValues)
        {
            modules.AddRange(Patterns[value]);
        }
        return modules.ToArray();
    }
}
