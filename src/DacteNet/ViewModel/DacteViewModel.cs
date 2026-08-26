namespace DacteNet.ViewModel;

/// <summary>
/// Flattened, print-ready view of one CT-e, computed by <see cref="DacteViewModelBuilder"/> from a
/// <see cref="Models.CteDocument"/> plus <see cref="DacteOptions"/>. Renderers (Rendering/A4,
/// Rendering/A5) only read from this object and never touch <see cref="Models.CteDocument"/> or
/// business rules directly - this is the seam between "what the document means" and "where it is drawn".
/// Field names mirror the ACBr Pascal component names quoted in retrato_layout.md/fastreport_crosscheck.md
/// where practical, to keep the mapping to the reference analysis traceable.
/// </summary>
public sealed class DacteViewModel
{
    public bool ModeloOS { get; set; }
    public bool CteSimplificado { get; set; } // always false - CT-e Simplificado is out of scope (kept for completeness/documentation)

    public string TituloDacte { get; set; } = "DACTE";
    public string SubtituloDacte { get; set; } = "";
    public string RotuloCTe { get; set; } = "CT-E";

    public string Modal { get; set; } = "";
    public string NumeroSerie { get; set; } = "";
    public string NumeroCTe { get; set; } = "";
    public string Serie { get; set; } = "";
    public string ChaveAcessoFormatada { get; set; } = "";
    public string ChaveAcessoDigitos { get; set; } = "";
    public string TipoImpressaoLegenda { get; set; } = "";

    public bool MostrarQrCode { get; set; }
    public string? QrCodeUrl { get; set; }

    public bool UsarBarcodeContingencia { get; set; }
    public string BarcodeDigitos { get; set; } = "";

    public string RotuloProtocolo { get; set; } = "PROTOCOLO DE AUTORIZAÇÃO DE USO";
    public string TextoProtocolo { get; set; } = "";
    public string RotuloDadosCte { get; set; } = "CHAVE DE ACESSO";

    public string DataHoraEmissao { get; set; } = "";
    public string NaturezaOperacao { get; set; } = "";
    public string Cfop { get; set; } = "";
    public string MunicipioInicio { get; set; } = "";
    public string MunicipioFim { get; set; } = "";
    public string TomaServicoIndicador { get; set; } = ""; // "SIM"/"NÃO" - CT-e globalizado
    public string ObservacaoFormaPagamento { get; set; } = "";
    public string RotuloTomaServicoIndicador { get; set; } = "TOMADOR DO SERVIÇO"; // toggles per version/modelo, see builder
    public string RotuloFormaPagamento { get; set; } = "FORMA DE PAGAMENTO";
    public string TipoCteTexto { get; set; } = "";
    public string TipoServicoTexto { get; set; } = "";
    /// <summary>
    /// TpTomadorToStrText(Toma03/Toma4) - who the tomador do serviço is, unconditionally (no v>=3.00
    /// "SIM"/"NÃO" globalizado toggle). Used by the A5 renderer's rllTomaServico, which - unlike A4's
    /// version-dependent rllTomaServico - always shows this text regardless of versão (confirmed in
    /// ACBrCTeDACTeRLRetratoA5.pas: `rllTomaServico.Caption := TpTomadorToStrText(...)`, no version
    /// branch at all).
    /// </summary>
    public string TomadorDescricaoTexto { get; set; } = "";

    public EnderecoVm Emitente { get; set; } = new();
    public string? EmitenteSite { get; set; }
    public string? EmitenteEmail { get; set; }

    public string? MensagemStatus { get; set; } // "SEM VALOR FISCAL" / cancelado / denegado / não enviado - watermark text

    // --- Dados do CT-e (remetente/destinatário/expedidor/recebedor/tomador) ---
    public EnderecoVm? Remetente { get; set; }
    public EnderecoVm? Destinatario { get; set; }
    public EnderecoVm? Expedidor { get; set; }
    public EnderecoVm? Recebedor { get; set; }
    public EnderecoVm? TomadorServico { get; set; }

    public string ProdutoPredominante { get; set; } = "";
    public string OutrasCaracteristicasCarga { get; set; } = "";
    public string ValorTotalCarga { get; set; } = "";

    // --- Peso / cubagem / volumes (rlb_04_DadosNotaFiscal) ---
    public List<LinhaMedidaVm> LinhasMedida { get; set; } = new();
    public string PesoBrutoKg { get; set; } = "";
    public string PesoBaseCalculoKg { get; set; } = "";
    public string PesoAferidoKg { get; set; } = "";
    public string CubagemM3 { get; set; } = "";

    // --- Seguro (legado <3.00, dentro de DadosNotaFiscal) ---
    public SeguroVm? SeguroLegado { get; set; }
    // --- Seguro (>=3.00, rlb_Dados_Seguradora - modelo 67 ou multimodal) ---
    public List<SeguroVm> SegurosModernos { get; set; } = new();

    // --- Complemento (tpCTe = Complemento) ---
    public List<string> ChavesComplementadas { get; set; } = new();

    // --- Produtos perigosos ---
    public List<ProdutoPerigosoVm> ProdutosPerigosos { get; set; } = new();

    // --- Veículos novos ---
    public List<VeiculoNovoVm> VeiculosNovos { get; set; } = new();

    // --- CT-e OS: Informações da prestação do serviço ---
    public string? InfoServicoQuantidade { get; set; }
    public string? InfoServicoDescricao { get; set; }

    // --- Valor da prestação ---
    public List<(string Nome, string Valor)> ComponentesPrestacao { get; set; } = new();
    public string ValorTotalServico { get; set; } = "";
    public string ValorTotalReceber { get; set; } = "";
    public IcmsVm Icms { get; set; } = new();
    public TributosFederaisVm? TributosFederais { get; set; }

    // --- Documentos originários ---
    public List<(string Tipo1, string DocOu2Campos1, string Tipo2, string DocOu2Campos2)> LinhasDocumentosOriginarios { get; set; } = new();
    public string TituloColunaSerie1 { get; set; } = "SÉRIE/NRO. DOCUMENTO";
    public string TituloColunaCnpj1 { get; set; } = "CNPJ/CPF EMITENTE";
    public string TituloColunaSerie2 { get; set; } = "SÉRIE/NRO. DOCUMENTO";
    public string TituloColunaCnpj2 { get; set; } = "CNPJ/CPF EMITENTE";

    // --- CT-e Anulado/Substituído ---
    public bool MostrarAnuladoSubstituido { get; set; }
    public string RotuloChaveAnuladoSubstituido { get; set; } = "";
    public string ChaveAnuladoSubstituido { get; set; } = "";
    public bool MostrarChaveAnulacaoSubstituicao { get; set; }
    public string ChaveAnulacaoSubstituicao { get; set; } = "";

    // --- Fluxo de carga (aéreo) ---
    public string? FluxoOrigem { get; set; }
    public string? FluxoDestino { get; set; }
    public string? FluxoRota { get; set; }

    // --- Observações ---
    public List<string> LinhasObservacoes { get; set; } = new();

    // --- Modal Rodoviário ---
    public ModalRodoviarioVm? ModalRodoviario { get; set; }
    // --- Modal Aéreo ---
    public ModalAereoVm? ModalAereo { get; set; }
    // --- Modal Aquaviário ---
    public ModalAquaviarioVm? ModalAquaviario { get; set; }

    // --- Dados de exclusivo uso do emissor / reservado ao Fisco ---
    public List<string> ObservacoesContribuinte { get; set; } = new();
    public List<string> ObservacoesFisco { get; set; } = new();
    public string? InformacoesAdicionaisFisco { get; set; }

    // --- Rodapé ---
    public string? RodapeDataHoraImpressao { get; set; }
    public string? RodapeUsuario { get; set; }
    public string? RodapeSistema { get; set; }

    public string? TextoResumoCanhoto { get; set; }
}

public sealed class EnderecoVm
{
    public string RazaoSocial { get; set; } = "";
    public string CnpjCpf { get; set; } = "";
    public string InscricaoEstadual { get; set; } = "";
    public string EnderecoLinha { get; set; } = "";
    public string Bairro { get; set; } = "";
    public string MunicipioUf { get; set; } = "";
    public string Cep { get; set; } = "";
    public string Fone { get; set; } = "";
    public string Pais { get; set; } = "";
    public List<string> LinhasEnderecoCompleto { get; set; } = new(); // pre-wrapped multi-line block for the header emitter box
}

public sealed class LinhaMedidaVm
{
    public string TipoMedida { get; set; } = "";
    public string UnidadeMedida { get; set; } = "";
    public string Quantidade { get; set; } = "";
}

public sealed class SeguroVm
{
    public string Responsavel { get; set; } = "";
    public string Seguradora { get; set; } = "";
    public string Apolice { get; set; } = "";
    public string Averbacao { get; set; } = "";
}

public sealed class ProdutoPerigosoVm
{
    public string NumeroOnu { get; set; } = "";
    public string NomeApropriado { get; set; } = "";
    public string ClasseRisco { get; set; } = "";
    public string GrupoEmbalagem { get; set; } = "";
    public string Quantidade { get; set; } = "";
}

public sealed class VeiculoNovoVm
{
    public string Chassi { get; set; } = "";
    public string Cor { get; set; } = "";
    public string Modelo { get; set; } = "";
    public string ValorUnitario { get; set; } = "";
    public string ValorFrete { get; set; } = "";
}

public sealed class IcmsVm
{
    public string SituacaoTributaria { get; set; } = "";
    public string BaseCalculo { get; set; } = "";
    public string Aliquota { get; set; } = "";
    public string ValorIcms { get; set; } = "";
    public string PercentualReducaoBc { get; set; } = "";
    public string IcmsStLegado { get; set; } = ""; // only populated for versão < 3.00, CST60
    public bool MostrarColunaReducaoBc { get; set; } = true;
    public bool MostrarColunaIcmsSt { get; set; }
}

public sealed class TributosFederaisVm
{
    public string Pis { get; set; } = "";
    public string Cofins { get; set; } = "";
    public string Ir { get; set; } = "";
    public string Inss { get; set; } = "";
    public string Csll { get; set; } = "";
}

public sealed class ModalRodoviarioVm
{
    public bool ModeloOS { get; set; }
    public string RntrcOuTaf { get; set; } = "";
    public string CiotOuRegistroEstadual { get; set; } = "";
    public string LotacaoTexto { get; set; } = ""; // "SIM"/"NÃO"
    public bool Lotacao { get; set; }
    public string DataPrevistaEntrega { get; set; } = "";
    public List<(string Tipo, string Placa, string Uf, string Rntrc)> Veiculos { get; set; } = new();
    public List<string> Motoristas { get; set; } = new();
    public List<string> Lacres { get; set; } = new();
    public List<(string CnpjFornecedora, string NumeroCompra, string CnpjPagador, string Valor)> ValesPedagio { get; set; } = new();
}

public sealed class ModalAereoVm
{
    public string Tarifa { get; set; } = "";
    public string RetiradaCarga { get; set; } = "";
    public string DetalheRetirada { get; set; } = "";
    public string NumeroOca { get; set; } = "";
    public string NumeroMinuta { get; set; } = "";
    public string ContaCorrente { get; set; } = "";
    public string Dimensoes { get; set; } = "";
    public string InstrucoesManuseio { get; set; } = "";
    public string InformacoesComplementares { get; set; } = "";
}

public sealed class ModalAquaviarioVm
{
    public string PortoEmbarque { get; set; } = "";
    public string PortoTransbordo { get; set; } = "";
    public string PortoDestino { get; set; } = "";
    public string Navio { get; set; } = "";
    public string Viagem { get; set; } = "";
    public string Balsas { get; set; } = "";
    public string Direcao { get; set; } = "";
    public string TipoNavegacao { get; set; } = "";
    public string ValorAfrmm { get; set; } = "";
    public string BaseCalculoAfrmm { get; set; } = "";
}
