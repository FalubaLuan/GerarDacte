namespace DacteNet.Models;

public sealed class PercursoItem
{
    public string? Uf { get; set; }
}

/// <summary>ide/toma03 or ide/toma3 or ide/toma (legacy shape: tomador is simply one of the four cargo-chain parties).</summary>
public sealed class Toma03
{
    public Tomador? Tomador { get; set; }
}

/// <summary>ide/toma4 (legacy shape used when the tomador is "Outros" - a party not otherwise on the CT-e).</summary>
public sealed class Toma4
{
    public Tomador? Tomador { get; set; }
    public string? CnpjCpf { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? RazaoSocial { get; set; }
    public string? NomeFantasia { get; set; }
    public string? Telefone { get; set; }
    public Endereco Endereco { get; set; } = new();
    public string? Email { get; set; }
}

/// <summary>infCTe/ide - identificação do CT-e.</summary>
public sealed class Identificacao
{
    public int? CodigoUF { get; set; }
    public int? CodigoCTe { get; set; }              // cCT
    public string? Cfop { get; set; }
    public string? NaturezaOperacao { get; set; }
    public ModeloDocumento? Modelo { get; set; }
    public int? Serie { get; set; }
    public int? NumeroCTe { get; set; }               // nCT
    public DateTimeOffset? DataHoraEmissao { get; set; }
    public TipoImpressao? TipoImpressao { get; set; }
    public TipoEmissao? TipoEmissao { get; set; }
    public int? DigitoVerificador { get; set; }
    public TipoAmbiente? TipoAmbiente { get; set; }
    public TipoCTe? TipoCTe { get; set; }
    public string? ProcessoEmissao { get; set; }
    public string? VersaoProcesso { get; set; }
    public IndicadorSimNao? IndicadorGlobalizado { get; set; }
    public string? ReferenciaCTe { get; set; }
    public int? CodigoMunicipioEnvio { get; set; }
    public string? MunicipioEnvio { get; set; }
    public string? UfEnvio { get; set; }
    public Modal? Modal { get; set; }
    public TipoServico? TipoServico { get; set; }
    public int? CodigoMunicipioInicio { get; set; }
    public string? MunicipioInicio { get; set; }
    public string? UfInicio { get; set; }
    public int? CodigoMunicipioFim { get; set; }
    public string? MunicipioFim { get; set; }
    public string? UfFim { get; set; }
    public Retira? Retira { get; set; }
    public string? DetalheRetira { get; set; }
    public IndicadorIeDestinatario? IndicadorIeTomador { get; set; }
    public DateTimeOffset? DataHoraSaidaOrigem { get; set; }
    public DateTimeOffset? DataHoraChegadaDestino { get; set; }
    public Toma03? Toma03 { get; set; }
    public Toma4? Toma4 { get; set; }
    public List<PercursoItem> Percurso { get; set; } = new();
    public DateTimeOffset? DataHoraContingencia { get; set; }
    public string? JustificativaContingencia { get; set; }

    public double Versao { get; set; }
}
