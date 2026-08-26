using System.Globalization;

namespace DacteNet.ViewModel;

/// <summary>
/// Text-formatting helpers mirroring ACBr's FormatarCNPJ/FormatarCPF/FormatarCEP/FormatarFone/
/// FormatarChaveAcesso/FormatFloatBr conventions (retrato_layout.md §5 "Masks used" / "CNPJ/CPF/IE/CEP/
/// phone/date formatting" - the exact mask patterns are stated there as "commonly-known ACBr convention,
/// not verified inside the two analyzed files", since the actual helpers live outside ACBrCTe proper).
/// </summary>
public static class Format
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static string OnlyDigits(string? s) => s is null ? "" : new string(s.Where(char.IsDigit).ToArray());

    public static string CnpjOuCpf(string? s)
    {
        var d = OnlyDigits(s);
        if (d.Length > 11) return Cnpj(d);
        if (d.Length > 0) return Cpf(d);
        return "";
    }

    public static string Cnpj(string? s)
    {
        var d = OnlyDigits(s).PadLeft(14, '0');
        if (d.Length != 14) return OnlyDigits(s);
        return $"{d[..2]}.{d[2..5]}.{d[5..8]}/{d[8..12]}-{d[12..]}";
    }

    public static string Cpf(string? s)
    {
        var d = OnlyDigits(s).PadLeft(11, '0');
        if (d.Length != 11) return OnlyDigits(s);
        return $"{d[..3]}.{d[3..6]}.{d[6..9]}-{d[9..]}";
    }

    public static string Cep(int? cep)
    {
        if (cep is null or 0) return "";
        var d = cep.Value.ToString(CultureInfo.InvariantCulture).PadLeft(8, '0');
        return $"{d[..5]}-{d[5..]}";
    }

    public static string Fone(string? s)
    {
        var d = OnlyDigits(s);
        return d.Length switch
        {
            <= 0 => "",
            10 => $"({d[..2]}) {d[2..6]}-{d[6..]}",
            11 => $"({d[..2]}) {d[2..7]}-{d[7..]}",
            8 => $"{d[..4]}-{d[4..]}",
            9 => $"{d[..5]}-{d[5..]}",
            _ => d,
        };
    }

    /// <summary>Groups a 44-digit access key into space-separated blocks of 4, ACBr's FormatarChaveAcesso convention.</summary>
    public static string ChaveAcesso(string? chave)
    {
        var d = OnlyDigits(chave);
        if (d.Length != 44) return d;
        var parts = new List<string>();
        for (int i = 0; i < 44; i += 4) parts.Add(d.Substring(i, 4));
        return string.Join(" ", parts);
    }

    /// <summary>Brazilian-locale fixed-decimal formatting - "FormatFloatBr(mskNxM, valor)" equivalent (thousand '.' / decimal ',').</summary>
    public static string Moeda(decimal? v, int decimals = 2) => (v ?? 0m).ToString("N" + decimals, PtBr);

    public static string Quantidade(decimal? v, int decimals = 4) => (v ?? 0m).ToString("N" + decimals, PtBr);

    public static string DataBr(DateTimeOffset? d) => d?.ToString("dd/MM/yyyy", PtBr) ?? "";

    public static string DataHoraBr(DateTimeOffset? d) => d?.ToString("dd/MM/yyyy HH:mm:ss", PtBr) ?? "";

    public static string HoraBr(DateTimeOffset? d) => d?.ToString("HH:mm", PtBr) ?? "";

    public static string NumeroDocumentoFiscal(int? n) => n?.ToString("000,000,000", PtBr) ?? "";
}
