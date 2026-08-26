namespace DacteNet.Models;

/// <summary>
/// Unified ICMS payload. Only the fields relevant to the CST actually in effect (<see cref="SituacaoTributaria"/>)
/// are populated by the XML parser; the rest stay null. This mirrors the ACBr Pascal model, which keeps one
/// sub-record per CST group permanently instantiated but meaningfully populates only the one matching the CST
/// (see cte_model.md §2 and the CarregaCalculoImposto excerpt in fastreport_crosscheck.md §2.2).
/// </summary>
public sealed class Icms
{
    public CstIcms SituacaoTributaria { get; set; }

    // CST 00 - Tributação normal
    public decimal? BaseCalculo { get; set; }         // vBC
    public decimal? AliquotaIcms { get; set; }         // pICMS
    public decimal? ValorIcms { get; set; }             // vICMS

    // CST 20 / 90 / ICMSOutraUF - redução de base de cálculo
    public decimal? PercentualReducaoBaseCalculo { get; set; }  // pRedBC / pRedBCOutraUF

    // CST 60 - ICMS cobrado por substituição tributária (retido anteriormente)
    public decimal? ValorCredito { get; set; }          // vCred (CST60/90)

    // ICMSUFFim - partilha do ICMS interestadual devido à UF de destino (DIFAL)
    public IcmsUfFim? UfFim { get; set; }

    // Simples Nacional
    public int? IndicadorSimplesNacional { get; set; }  // indSN

    public decimal? ValorTotalTributos { get; set; }     // imp/vTotTrib
    public string? InformacoesAdicionaisFisco { get; set; } // imp/infAdFisco
}

public sealed class IcmsUfFim
{
    public decimal? BaseCalculo { get; set; }             // vBCUFFim
    public decimal? PercentualFcp { get; set; }            // pFCPUFFim
    public decimal? AliquotaInterna { get; set; }           // pICMSUFFim
    public decimal? AliquotaInterestadual { get; set; }      // pICMSInter
    public decimal? PercentualPartilha { get; set; }          // pICMSInterPart
    public decimal? ValorFcp { get; set; }                     // vFCPUFFim
    public decimal? ValorIcmsUFFim { get; set; }                // vICMSUFFim
    public decimal? ValorIcmsUFIni { get; set; }                 // vICMSUFIni
}

public sealed class InfoTributosFederais
{
    public decimal? Pis { get; set; }
    public decimal? Cofins { get; set; }
    public decimal? Ir { get; set; }
    public decimal? Inss { get; set; }
    public decimal? Csll { get; set; }
}
