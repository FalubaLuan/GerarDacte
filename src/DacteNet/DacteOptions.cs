using DacteNet.Models;
using DacteNet.Rendering.Primitives;

namespace DacteNet;

public enum PosicaoCanhoto { Cabecalho, Rodape }
public enum LayoutCanhoto { Padrao, Barra }

/// <summary>
/// Print-time configuration - the C# equivalent of the properties ACBr keeps on the DACTE *component*
/// (TACBrCTeDACTeRL/TACBrCTeDACTEFR) rather than on the CT-e document itself: operator/software
/// preferences, not data that comes from the XML (see retrato_layout.md §7, last bullet). None of this
/// is read from the CT-e XML.
/// </summary>
public sealed class DacteOptions
{
    public TamanhoPapel TamanhoPapel { get; set; } = TamanhoPapel.A4;

    public PosicaoCanhoto PosicaoCanhoto { get; set; } = PosicaoCanhoto.Cabecalho;
    public LayoutCanhoto LayoutCanhoto { get; set; } = LayoutCanhoto.Padrao;
    public bool ExibirResumoCanhoto { get; set; }

    /// <summary>Overrides the "protocolo de autorização" text shown - normally left null so the value from procCTe/XML is used.</summary>
    public string? Protocolo { get; set; }

    /// <summary>Forces the "CT-e CANCELADO" banner even if the XML/protocol doesn't itself say so (mirrors ACBr's own Cancelada flag).</summary>
    public bool Cancelada { get; set; }

    public bool ImprimirHoraSaida { get; set; }
    public string? ImprimirHoraSaidaHora { get; set; }

    /// <summary>Name of the emitting application/system, printed in the footer strip.</summary>
    public string? Sistema { get; set; }
    public string? Usuario { get; set; }

    public string? Site { get; set; }
    public string? Email { get; set; }

    /// <summary>Issuer logo - JPEG only (see PdfImage.FromJpegBytes and docs/limitations.md).</summary>
    public PdfImage? Logo { get; set; }
    public bool ExpandeLogoMarca { get; set; }

    /// <summary>Optional custom full-bleed watermark image drawn behind the "Dados do DACTE" block (independent of the homologação text watermark).</summary>
    public PdfImage? MarcaDeAgua { get; set; }
}
