namespace DacteNet.Models;

public sealed class OcorrenciaRodoviaria
{
    public string? Serie { get; set; }
    public int? Numero { get; set; }
    public DateTimeOffset? DataEmissao { get; set; }
    public string? EmissorCnpj { get; set; }
    public string? EmissorUf { get; set; }
}

public sealed class ValePedagio
{
    public string? CnpjFornecedora { get; set; }
    public string? NumeroCompra { get; set; }
    public string? CnpjPagador { get; set; }
    public decimal? Valor { get; set; }
}

public sealed class ProprietarioVeiculo
{
    public string? CnpjCpf { get; set; }
    public string? Rntrc { get; set; }
    public string? Nome { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? Uf { get; set; }
}

public sealed class VeiculoRodoviario
{
    public string? CodigoInterno { get; set; }
    public string? Renavam { get; set; }
    public string? Placa { get; set; }
    public int? TaraKg { get; set; }
    public int? CapacidadeKg { get; set; }
    public int? CapacidadeM3 { get; set; }
    public TipoPropriedadeVeiculo? TipoPropriedade { get; set; }
    public TipoVeiculo? TipoVeiculo { get; set; }
    public string? TipoRodado { get; set; }
    public string? TipoCarroceria { get; set; }
    public string? Uf { get; set; }
    public ProprietarioVeiculo? Proprietario { get; set; }
}

public sealed class MotoristaItem
{
    public string? Nome { get; set; }
    public string? Cpf { get; set; }
}

/// <summary>infCTeNorm/rodo - modal Rodoviário (padrão, não-OS).</summary>
public sealed class ModalRodoviario
{
    public string? Rntrc { get; set; }
    public DateTimeOffset? DataPrevistaEntrega { get; set; }
    public Lotacao? Lotacao { get; set; }
    public string? Ciot { get; set; }
    public List<OcorrenciaRodoviaria> Ocorrencias { get; set; } = new();
    public List<ValePedagio> ValesPedagio { get; set; } = new();
    public List<VeiculoRodoviario> Veiculos { get; set; } = new();
    public List<LacreItem> Lacres { get; set; } = new();
    public List<MotoristaItem> Motoristas { get; set; } = new();
}

public sealed class ProprietarioVeiculoOS
{
    public string? CnpjCpf { get; set; }
    public string? Taf { get; set; }
    public string? NumeroRegistroEstadual { get; set; }
    public string? Nome { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? Uf { get; set; }
}

public sealed class VeiculoRodoviarioOS
{
    public string? Placa { get; set; }
    public string? Renavam { get; set; }
    public string? Uf { get; set; }
    public ProprietarioVeiculoOS? Proprietario { get; set; }
}

/// <summary>infCTeNorm/rodoOS - modal Rodoviário, exclusivo para CT-e OS (modelo 67).</summary>
public sealed class ModalRodoviarioOS
{
    public string? Taf { get; set; }
    public string? NumeroRegistroEstadual { get; set; }
    public VeiculoRodoviarioOS? Veiculo { get; set; }
    public TipoFretamento? TipoFretamento { get; set; }
    public DateTimeOffset? DataHoraViagem { get; set; }
}

public sealed class TarifaAerea
{
    public string? Classe { get; set; }        // CL
    public string? Codigo { get; set; }          // cTar
    public decimal? Valor { get; set; }           // vTar
}

/// <summary>infCTeNorm/aereo - modal Aéreo.</summary>
public sealed class ModalAereo
{
    public int? NumeroMinuta { get; set; }
    public string? NumeroOCA { get; set; }
    public DateTimeOffset? DataPrevistaEntrega { get; set; }
    public string? LocalAgenciaEmissao { get; set; }
    public string? IdentificacaoTerminal { get; set; }
    public TarifaAerea? Tarifa { get; set; }
    public string? Dimensoes { get; set; }               // natCarga/xDime
    public List<string> InstrucoesManuseio { get; set; } = new(); // cInfManu[] -> already text-mapped
    public string? InformacoesComplementaresManuseio { get; set; } // cIMP
}

public sealed class ContainerAquaviario
{
    public string? Numero { get; set; }
    public List<LacreItem> Lacres { get; set; } = new();
}

/// <summary>infCTeNorm/aquav - modal Aquaviário.</summary>
public sealed class ModalAquaviario
{
    public decimal? ValorPrestacao { get; set; }
    public decimal? ValorAfrmm { get; set; }
    public string? NumeroBooking { get; set; }
    public string? NumeroControle { get; set; }
    public string? NomeNavio { get; set; }
    public string? NumeroViagem { get; set; }
    public DirecaoAquaviaria? Direcao { get; set; }
    public string? PortoEmbarque { get; set; }
    public string? PortoTransbordo { get; set; }
    public string? PortoDestino { get; set; }
    public TipoNavegacao? TipoNavegacao { get; set; }
    public string? Irin { get; set; }
    public List<string> Balsas { get; set; } = new();
    public List<ContainerAquaviario> Containeres { get; set; } = new();
}

public sealed class VagaoFerroviario
{
    public int? Numero { get; set; }
    public decimal? Capacidade { get; set; }
    public string? Tipo { get; set; }
    public decimal? PesoReal { get; set; }
    public decimal? PesoBaseCalculo { get; set; }
}

/// <summary>infCTeNorm/ferrov - modal Ferroviário.</summary>
public sealed class ModalFerroviario
{
    public TipoTrafegoFerroviario? TipoTrafego { get; set; }
    public string? Fluxo { get; set; }
    public string? IdentificacaoTrem { get; set; }
    public decimal? ValorFrete { get; set; }
    public string? ChaveCteFerroOrigem { get; set; }
    public List<VagaoFerroviario> Vagoes { get; set; } = new();
}

/// <summary>infCTeNorm/duto - modal Dutoviário.</summary>
public sealed class ModalDutoviario
{
    public decimal? ValorTarifa { get; set; }
    public DateTimeOffset? DataInicio { get; set; }
    public DateTimeOffset? DataFim { get; set; }
    public ClasseDuto? Classe { get; set; }
}

/// <summary>infCTeNorm/multimodal.</summary>
public sealed class ModalMultimodal
{
    public string? CertificadoOperador { get; set; }  // COTM
    public bool? Negociavel { get; set; }
    public string? NomeSeguradora { get; set; }
    public string? CnpjSeguradora { get; set; }
    public string? NumeroApolice { get; set; }
    public string? NumeroAverbacao { get; set; }
}
