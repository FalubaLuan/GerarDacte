namespace DacteNet.Models;

public sealed class QuantidadeCarga
{
    public UnidadeMedidaCarga Unidade { get; set; }   // cUnid
    public string? TipoMedida { get; set; }             // tpMed - free text ("PESO BRUTO", "PESO BASE DE CALCULO", ...)
    public decimal Quantidade { get; set; }              // qCarga
}

/// <summary>infCTeNorm/infCarga (or the CT-e-Simplificado top-level infCarga).</summary>
public sealed class InformacoesCarga
{
    public decimal? ValorCarga { get; set; }          // vCarga
    public string? ProdutoPredominante { get; set; }   // proPred
    public string? OutrasCaracteristicas { get; set; } // xOutCat
    public List<QuantidadeCarga> Quantidades { get; set; } = new(); // infQ[]
    public decimal? ValorCargaAverbacao { get; set; } // vCargaAverb
}

public sealed class LacreItem
{
    public string? Numero { get; set; }   // nLacre
}

public sealed class UnidadeTransporte
{
    public string? Tipo { get; set; }         // tpUnidTransp
    public string? Identificacao { get; set; } // idUnidTransp
    public List<LacreItem> Lacres { get; set; } = new();
    public double? QuantidadeRateada { get; set; } // qtdRat
}

public sealed class UnidadeCarga
{
    public string? Tipo { get; set; }          // tpUnidCarga
    public string? Identificacao { get; set; }  // idUnidCarga
    public List<LacreItem> Lacres { get; set; } = new();
    public double? QuantidadeRateada { get; set; } // qtdRat
}

/// <summary>infDoc/infNF - documento fiscal em papel (NF modelo 1/1A ou avulsa/produtor).</summary>
public sealed class DocumentoNFPapel
{
    public string? NumeroRoma { get; set; }
    public string? NumeroPedido { get; set; }
    public string? Modelo { get; set; }
    public string? Serie { get; set; }
    public string? Numero { get; set; }       // nDoc
    public DateTimeOffset? DataEmissao { get; set; }
    public decimal? BaseCalculo { get; set; }
    public decimal? ValorIcms { get; set; }
    public decimal? BaseCalculoST { get; set; }
    public decimal? ValorST { get; set; }
    public decimal? ValorProdutos { get; set; }
    public decimal? ValorNF { get; set; }
    public int? Cfop { get; set; }
    public decimal? PesoTotal { get; set; }   // nPeso
    public string? Pin { get; set; }
    public DateTimeOffset? DataPrevista { get; set; }
}

/// <summary>infDoc/infNFe - NF-e vinculada (referenciada pela chave de 44 dígitos).</summary>
public sealed class DocumentoNFe
{
    public string? Chave { get; set; }
    public string? Pin { get; set; }
    public DateTimeOffset? DataPrevista { get; set; }
}

/// <summary>infDoc/infOutros - outros documentos (declaração, CF-e SAT, NFC-e, dutoviário, outros).</summary>
public sealed class DocumentoOutros
{
    public TipoDocumentoOutros Tipo { get; set; }
    public string? DescricaoOutros { get; set; }
    public string? Numero { get; set; }
    public DateTimeOffset? DataEmissao { get; set; }
    public decimal? ValorDocumentoFiscal { get; set; }
    public DateTimeOffset? DataPrevista { get; set; }
}

public sealed class DocumentoAnteriorEletronico
{
    public string? Chave { get; set; }  // chCTe (>=3.00) / chave (legado)
}

public sealed class DocumentoAnteriorPapel
{
    public TipoDocumentoAnteriorPapel Tipo { get; set; }
    public string? Serie { get; set; }
    public string? SubSerie { get; set; }
    public string? Numero { get; set; }
    public DateTimeOffset? DataEmissao { get; set; }
}

public sealed class EmissorDocumentoAnterior
{
    public string? CnpjCpf { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? Uf { get; set; }
    public string? Nome { get; set; }
    public List<DocumentoAnteriorPapel> DocumentosPapel { get; set; } = new();
    public List<DocumentoAnteriorEletronico> DocumentosEletronicos { get; set; } = new();
}

/// <summary>infCTeNorm/infDoc - documentos originários transportados.</summary>
public sealed class DocumentosOriginarios
{
    public List<DocumentoNFPapel> NotasFiscais { get; set; } = new();
    public List<DocumentoNFe> NotasFiscaisEletronicas { get; set; } = new();
    public List<DocumentoOutros> Outros { get; set; } = new();
}

public sealed class SeguroCarga
{
    public ResponsavelSeguro? Responsavel { get; set; }
    public string? NomeSeguradora { get; set; }
    public string? NumeroApolice { get; set; }
    public string? NumeroAverbacao { get; set; }
    public decimal? ValorCarga { get; set; }
}
