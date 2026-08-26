namespace DacteNet.Models;

public sealed class DuplicataItem
{
    public string? Numero { get; set; }
    public DateTimeOffset? Vencimento { get; set; }
    public decimal? Valor { get; set; }
}

/// <summary>infCTeNorm/cobr - fatura/duplicatas (pouco usado no DACTE mas mantido para fidelidade).</summary>
public sealed class Cobranca
{
    public string? NumeroFatura { get; set; }
    public decimal? ValorOriginal { get; set; }
    public decimal? ValorDesconto { get; set; }
    public decimal? ValorLiquido { get; set; }
    public List<DuplicataItem> Duplicatas { get; set; } = new();
}

/// <summary>infCTeNorm/infCTeSub - dados do CT-e sendo substituído.</summary>
public sealed class InfoCteSubstituto
{
    public string? ChaveCteSubstituido { get; set; }
    public string? ChaveCteAnulacao { get; set; }
}

/// <summary>chave(s) do(s) CT-e complementado(s) (tpCTe = Complemento) - infCteComp (&lt;=3.00, único) / infCteComp10 (&gt;=4.00, lista).</summary>
public sealed class InfoCteComplementado
{
    public List<string> ChavesComplementadas { get; set; } = new();
}

/// <summary>infCTeAnu - CT-e sendo anulado (tpCTe = Anulacao).</summary>
public sealed class InfoCteAnulado
{
    public string? Chave { get; set; }
    public DateTimeOffset? DataEmissao { get; set; }
}

public sealed class ProdutoPerigoso
{
    public string? NumeroOnu { get; set; }
    public string? NomeApropriado { get; set; }
    public string? ClasseRisco { get; set; }
    public string? GrupoEmbalagem { get; set; }
    public string? QuantidadeTotalProduto { get; set; }
    public string? QuantidadeVolumoTipo { get; set; }
    public string? PontoFulgor { get; set; }
    public string? QuantidadeTotalEmbalagem { get; set; }
}

public sealed class VeiculoNovo
{
    public string? Chassi { get; set; }
    public string? CodigoCor { get; set; }
    public string? Cor { get; set; }
    public string? CodigoModelo { get; set; }
    public decimal? ValorUnitario { get; set; }
    public decimal? ValorFrete { get; set; }
}

public sealed class InformacoesServico
{
    public string? DescricaoServico { get; set; }
    public decimal? QuantidadeCarga { get; set; }
}

/// <summary>infRespTec - responsável técnico (software house). Não impresso na maioria dos layouts do DACTE.</summary>
public sealed class ResponsavelTecnico
{
    public string? Cnpj { get; set; }
    public string? Contato { get; set; }
    public string? Email { get; set; }
    public string? Telefone { get; set; }
}

/// <summary>protCTe/infProt - protocolo de autorização (só existe quando o XML está envelopado em cteProc/procCTe).</summary>
public sealed class ProtocoloAutorizacao
{
    public TipoAmbiente? TipoAmbiente { get; set; }
    public string? VersaoAplicativo { get; set; }
    public string? ChaveAcesso { get; set; }
    public DateTimeOffset? DataHoraRecebimento { get; set; }
    public string? NumeroProtocolo { get; set; }
    public int? CodigoStatus { get; set; }
    public string? MotivoStatus { get; set; }
}
