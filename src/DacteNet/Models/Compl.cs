namespace DacteNet.Models;

public sealed class ObservacaoItem
{
    public string? Campo { get; set; }   // xCampo
    public string? Texto { get; set; }   // xTexto
}

public sealed class FluxoCarga
{
    public string? Origem { get; set; }        // xOrig
    public List<string> Passagens { get; set; } = new();  // pass[]/xPass
    public string? Destino { get; set; }        // xDest
    public string? Rota { get; set; }            // xRota
}

public sealed class EntregaProgramada
{
    public TipoDataEntrega TipoData { get; set; } = TipoDataEntrega.NaoInformado;
    public TipoHoraEntrega TipoHora { get; set; } = TipoHoraEntrega.NaoInformado;

    // semData/comData/noPeriodo share one "tpPer" text plus optional dates
    public string? TipoPeriodoTexto { get; set; }
    public DateTimeOffset? DataProgramada { get; set; }   // comData/dProg
    public DateTimeOffset? DataInicio { get; set; }        // noPeriodo/dIni
    public DateTimeOffset? DataFim { get; set; }           // noPeriodo/dFim

    public string? TipoHorarioTexto { get; set; }
    public DateTimeOffset? HoraProgramada { get; set; }    // comHora/hProg
    public DateTimeOffset? HoraInicio { get; set; }         // noInter/hIni
    public DateTimeOffset? HoraFim { get; set; }            // noInter/hFim
}

/// <summary>infCTe/compl - complemento.</summary>
public sealed class Complemento
{
    public string? CaracteristicaAdicional { get; set; }    // xCaracAd
    public string? CaracteristicaServico { get; set; }       // xCaracSer
    public string? DescricaoEmissao { get; set; }             // xEmi
    public FluxoCarga? Fluxo { get; set; }
    public EntregaProgramada? Entrega { get; set; }
    public string? OrigemCalculo { get; set; }    // origCalc
    public string? DestinoCalculo { get; set; }    // destCalc
    public string? Observacoes { get; set; }        // xObs
    public List<ObservacaoItem> ObservacoesContribuinte { get; set; } = new();
    public List<ObservacaoItem> ObservacoesFisco { get; set; } = new();
}
