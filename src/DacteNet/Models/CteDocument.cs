namespace DacteNet.Models;

/// <summary>
/// infCTeNorm - corpo "normal" do CT-e (usado quando ide.TipoCTe = Normal/Complemento/Anulacao/Substituto).
/// Só um dos sete objetos modais abaixo é relevante por vez, selecionado por Ide.Modal — replica a
/// composição do Pascal (todos os sub-objetos sempre existem; só o que combina com ide/modal é
/// significativo). Ver cte_model.md §2.
/// </summary>
public sealed class InfoCTeNormal
{
    public string? ChaveCteCancelado { get; set; }   // refCTeCanc
    public InformacoesServico? InformacoesServico { get; set; }
    public InformacoesCarga InformacoesCarga { get; set; } = new();
    public DocumentosOriginarios DocumentosOriginarios { get; set; } = new();
    public List<EmissorDocumentoAnterior> DocumentosAnteriores { get; set; } = new();
    public List<SeguroCarga> Seguros { get; set; } = new();

    public ModalRodoviario? Rodoviario { get; set; }
    public ModalRodoviarioOS? RodoviarioOS { get; set; }
    public ModalAereo? Aereo { get; set; }
    public ModalAquaviario? Aquaviario { get; set; }
    public ModalFerroviario? Ferroviario { get; set; }
    public ModalDutoviario? Dutoviario { get; set; }
    public ModalMultimodal? Multimodal { get; set; }

    public List<ProdutoPerigoso> ProdutosPerigosos { get; set; } = new();
    public List<VeiculoNovo> VeiculosNovos { get; set; } = new();
    public Cobranca? Cobranca { get; set; }
    public InfoCteSubstituto? CteSubstituto { get; set; }
    public string? ObservacoesGlobalizado { get; set; }  // infGlobalizado/xObs
}

/// <summary>
/// Documento CT-e completo (infCTe), pronto para geração do DACTE. Correspondência com o modelo Pascal
/// TCTe documentada em /home/claude/work/analysis/cte_model.md. Suporta CT-e (modelo 57) e CT-e OS
/// (modelo 67); CT-e Simplificado (modelo 57 com tpCTe Simplificado) e GTVe (modelo 64) NÃO são
/// suportados por esta biblioteca - ver docs/limitations.md.
/// </summary>
public sealed class CteDocument
{
    /// <summary>Identificador completo do CT-e (atributo Id de infCTe, ex.: "CTe35190512345678000112570010000000011000000015").</summary>
    public string Id { get; set; } = "";

    /// <summary>Chave de acesso: os 44 dígitos numéricos de <see cref="Id"/>, sem o prefixo literal "CTe".</summary>
    public string ChaveAcesso => Id.Length >= 3 && Id.StartsWith("CTe", StringComparison.OrdinalIgnoreCase)
        ? Id[3..]
        : new string(Id.Where(char.IsDigit).ToArray());

    public Identificacao Identificacao { get; set; } = new();
    public Complemento? Complemento { get; set; }
    public Emitente Emitente { get; set; } = new();

    /// <summary>infCTe/toma - grupo dedicado do tomador (versão &gt;= 3.00 / CT-e OS / CT-e Simplificado).</summary>
    public TomadorServico? Tomador { get; set; }

    public Remetente? Remetente { get; set; }
    public Expedidor? Expedidor { get; set; }
    public Recebedor? Recebedor { get; set; }
    public Destinatario? Destinatario { get; set; }

    public ValorPrestacao ValorPrestacao { get; set; } = new();
    public Icms Icms { get; set; } = new();
    public InfoTributosFederais? TributosFederais { get; set; }

    public InfoCTeNormal? InfoNormal { get; set; }

    public InfoCteComplementado? CteComplementado { get; set; }
    public InfoCteAnulado? CteAnulado { get; set; }

    public ResponsavelTecnico? ResponsavelTecnico { get; set; }

    /// <summary>infCTeSupl/qrCodCTe - URL para o QR Code impresso no DACTE, quando presente no XML.</summary>
    public string? QrCodeUrl { get; set; }

    /// <summary>protCTe/infProt - presente apenas quando o XML fornecido está envelopado (cteProc/procCTe). Nulo para um CT-e "bare", ainda sem protocolo.</summary>
    public ProtocoloAutorizacao? Protocolo { get; set; }
}
