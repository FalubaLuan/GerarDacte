using DacteNet.Models;

namespace DacteNet.Xml;

/// <summary>
/// String-to-enum conversions mirroring ACBr's StrToXxx helpers in pcteConversaoCTe.pas, using the
/// public CT-e XSD code values (confirmed against cte_model.md §3.1 where ACBr's own source carried the
/// value, and against the official CT-e schema documentation otherwise - see cte_model.md §3.2 for which
/// enums that applies to). Every parser defaults leniently rather than throwing, mirroring ACBr's own
/// "silently default on unknown/missing" behaviour (xml_mapping.md §2/§4) - a genuinely malformed
/// document is expected to have already failed on the two ACBr-style hard checks (missing Id / versao).
/// </summary>
internal static class CteEnumParsers
{
    public static TipoAmbiente? TipoAmbiente(string? s) => s switch
    {
        "1" => Models.TipoAmbiente.Producao,
        "2" => Models.TipoAmbiente.Homologacao,
        _ => null
    };

    public static TipoEmissao? TipoEmissao(string? s) => s switch
    {
        "1" => Models.TipoEmissao.Normal,
        "2" => Models.TipoEmissao.Contingencia,
        "3" => Models.TipoEmissao.SCAN,
        "4" => Models.TipoEmissao.DPEC,
        "5" => Models.TipoEmissao.FSDA,
        "6" => Models.TipoEmissao.SVCAN,
        "7" => Models.TipoEmissao.SVCRS,
        "8" => Models.TipoEmissao.SVCSP,
        "9" => Models.TipoEmissao.OffLine,
        _ => null
    };

    public static ModeloDocumento? Modelo(string? s) => s switch
    {
        "57" => ModeloDocumento.CTe,
        "64" => ModeloDocumento.GTVe,
        "67" => ModeloDocumento.CTeOS,
        _ => null
    };

    public static TipoCTe? TipoCTe(string? s) => s switch
    {
        "0" => Models.TipoCTe.Normal,
        "1" => Models.TipoCTe.Complemento,
        "2" => Models.TipoCTe.Anulacao,
        "3" => Models.TipoCTe.Substituto,
        "4" => Models.TipoCTe.GTVe,
        "5" => Models.TipoCTe.CTeSimplificado,
        "6" => Models.TipoCTe.SubstitutoCTeSimplificado,
        _ => null
    };

    public static TipoServico? TipoServico(string? s) => s switch
    {
        "0" => Models.TipoServico.Normal,
        "1" => Models.TipoServico.Subcontratacao,
        "2" => Models.TipoServico.Redespacho,
        "3" => Models.TipoServico.RedespachoIntermediario,
        "4" => Models.TipoServico.Multimodal,
        "6" => Models.TipoServico.TransportePessoas,
        "7" => Models.TipoServico.TransporteValores,
        "8" => Models.TipoServico.ExcessoBagagem,
        "9" => Models.TipoServico.GTV,
        _ => null
    };

    public static Modal? Modal(string? s) => s switch
    {
        "01" or "1" => Models.Modal.Rodoviario,
        "02" or "2" => Models.Modal.Aereo,
        "03" or "3" => Models.Modal.Aquaviario,
        "04" or "4" => Models.Modal.Ferroviario,
        "05" or "5" => Models.Modal.Dutoviario,
        "06" or "6" => Models.Modal.Multimodal,
        _ => null
    };

    public static TipoImpressao? TipoImpressao(string? s) => s switch
    {
        "1" => Models.TipoImpressao.Retrato,
        "2" => Models.TipoImpressao.Paisagem,
        _ => null
    };

    public static Tomador? Tomador(string? s) => s switch
    {
        "0" => Models.Tomador.Remetente,
        "1" => Models.Tomador.Expedidor,
        "2" => Models.Tomador.Recebedor,
        "3" => Models.Tomador.Destinatario,
        "4" => Models.Tomador.Outros,
        _ => null
    };

    public static IndicadorSimNao? IndicadorSimNao(string? s) => s switch
    {
        "0" => Models.IndicadorSimNao.Nao,
        "1" => Models.IndicadorSimNao.Sim,
        _ => null
    };

    public static IndicadorIeDestinatario? IndicadorIeDestinatario(string? s) => s switch
    {
        "1" => Models.IndicadorIeDestinatario.ContribuinteICMS,
        "2" => Models.IndicadorIeDestinatario.ContribuinteIsento,
        "9" => Models.IndicadorIeDestinatario.NaoContribuinte,
        _ => null
    };

    public static Retira? Retira(string? s) => s switch
    {
        "0" => Models.Retira.Sim,
        "1" => Models.Retira.Nao,
        _ => null
    };

    public static RegimeTributario? RegimeTributario(string? s) => s switch
    {
        "1" => Models.RegimeTributario.SimplesNacional,
        "2" => Models.RegimeTributario.SimplesNacionalExcessoReceita,
        "3" => Models.RegimeTributario.RegimeNormal,
        "4" => Models.RegimeTributario.SimplesNacionalMEI,
        _ => Models.RegimeTributario.Nenhum
    };

    public static ResponsavelSeguro? ResponsavelSeguro(string? s) => s switch
    {
        "0" => Models.ResponsavelSeguro.Remetente,
        "1" => Models.ResponsavelSeguro.Expedidor,
        "2" => Models.ResponsavelSeguro.Recebedor,
        "3" => Models.ResponsavelSeguro.Destinatario,
        "4" => Models.ResponsavelSeguro.EmitenteCTe,
        "5" => Models.ResponsavelSeguro.TomadorServico,
        _ => null
    };

    // infQ/cUnid - SEFAZ CT-e "Tabela de Unidade de Medida" (external to the two analyzed DACTE
    // files; TUnidMed itself lives in a shared pcn* unit not present in this checkout - see
    // analysis/cte_model.md §3.2). Order confirmed 00=M3/01=KG by cross-checking against this
    // library's own test fixture (cubagem uses cUnid=00, peso uses cUnid=01) - a previous version of
    // this mapping had 00=KG/01=TON, which silently multiplied peso values by 1000 on render.
    public static UnidadeMedidaCarga UnidadeMedidaCarga(string? s) => s switch
    {
        "00" or "0" => Models.UnidadeMedidaCarga.M3,
        "01" or "1" => Models.UnidadeMedidaCarga.Kg,
        "02" or "2" => Models.UnidadeMedidaCarga.Ton,
        "03" or "3" => Models.UnidadeMedidaCarga.Unidade,
        "04" or "4" => Models.UnidadeMedidaCarga.Litros,
        "05" or "5" => Models.UnidadeMedidaCarga.MMBTU,
        _ => Models.UnidadeMedidaCarga.Desconhecida
    };

    public static TipoDocumentoOutros TipoDocumentoOutros(string? s) => s switch
    {
        "00" => Models.TipoDocumentoOutros.Declaracao,
        "10" => Models.TipoDocumentoOutros.Dutoviario,
        "59" => Models.TipoDocumentoOutros.CFeSAT,
        "65" => Models.TipoDocumentoOutros.NFCe,
        "99" => Models.TipoDocumentoOutros.Outros,
        _ => Models.TipoDocumentoOutros.NaoInformado
    };

    public static TipoDocumentoAnteriorPapel TipoDocumentoAnteriorPapel(string? s) => s switch
    {
        "00" => Models.TipoDocumentoAnteriorPapel.CTRC,
        "01" => Models.TipoDocumentoAnteriorPapel.CTAC,
        "02" => Models.TipoDocumentoAnteriorPapel.ACT,
        "03" => Models.TipoDocumentoAnteriorPapel.NF7,
        "04" => Models.TipoDocumentoAnteriorPapel.NF27,
        "05" => Models.TipoDocumentoAnteriorPapel.CAN,
        "06" => Models.TipoDocumentoAnteriorPapel.CTMC,
        "07" => Models.TipoDocumentoAnteriorPapel.ATRE,
        "08" => Models.TipoDocumentoAnteriorPapel.DTA,
        "09" => Models.TipoDocumentoAnteriorPapel.CAI,
        "10" => Models.TipoDocumentoAnteriorPapel.CCPI,
        "11" => Models.TipoDocumentoAnteriorPapel.CA,
        "12" => Models.TipoDocumentoAnteriorPapel.TIF,
        "13" => Models.TipoDocumentoAnteriorPapel.BL,
        _ => Models.TipoDocumentoAnteriorPapel.Outros
    };

    public static TipoVeiculo? TipoVeiculo(string? s) => s switch
    {
        "0" => Models.TipoVeiculo.Tracao,
        "1" => Models.TipoVeiculo.Reboque,
        _ => null
    };

    public static TipoPropriedadeVeiculo? TipoPropriedadeVeiculo(string? s) => s switch
    {
        "P" => Models.TipoPropriedadeVeiculo.Proprio,
        "T" => Models.TipoPropriedadeVeiculo.Terceiro,
        _ => null
    };

    public static Lotacao? Lotacao(string? s) => s switch
    {
        "0" => Models.Lotacao.Nao,
        "1" => Models.Lotacao.Sim,
        _ => null
    };

    public static TipoNavegacao? TipoNavegacao(string? s) => s switch
    {
        "0" => Models.TipoNavegacao.Interior,
        "1" => Models.TipoNavegacao.Cabotagem,
        _ => null
    };

    public static DirecaoAquaviaria? Direcao(string? s) => s switch
    {
        "N" => DirecaoAquaviaria.Norte,
        "L" => DirecaoAquaviaria.Leste,
        "S" => DirecaoAquaviaria.Sul,
        "O" => DirecaoAquaviaria.Oeste,
        _ => null
    };

    public static TipoTrafegoFerroviario? TipoTrafego(string? s) => s switch
    {
        "0" => Models.TipoTrafegoFerroviario.Proprio,
        "1" => Models.TipoTrafegoFerroviario.Mutuo,
        "2" => Models.TipoTrafegoFerroviario.Rodoferroviario,
        "3" => Models.TipoTrafegoFerroviario.Rodoviario,
        _ => null
    };

    public static ClasseDuto? ClasseDuto(string? s) => s switch
    {
        "1" => Models.ClasseDuto.Gasoduto,
        "2" => Models.ClasseDuto.Mineroduto,
        "3" => Models.ClasseDuto.Oleoduto,
        _ => Models.ClasseDuto.Nenhum
    };

    public static TipoFretamento? TipoFretamento(string? s) => s switch
    {
        "1" => Models.TipoFretamento.Eventual,
        "2" => Models.TipoFretamento.Continuo,
        _ => Models.TipoFretamento.Nenhum
    };

    /// <summary>Maps the ide/ICMS 'CST'/'CSOSN' text (already dispatched to the right group by the caller) to our synthetic CstIcms discriminator.</summary>
    public static CstIcms CstIcms(string groupTag, string? cst) => groupTag switch
    {
        "ICMS00" => Models.CstIcms.Cst00,
        "ICMS20" => Models.CstIcms.Cst20,
        "ICMS45" => cst switch
        {
            "40" => Models.CstIcms.Cst40,
            "41" => Models.CstIcms.Cst41,
            "51" => Models.CstIcms.Cst51,
            _ => Models.CstIcms.Cst45
        },
        "ICMS60" => Models.CstIcms.Cst60,
        "ICMS90" => Models.CstIcms.Cst90,
        "ICMSOutraUF" => Models.CstIcms.IcmsOutraUF,
        "ICMSSN" => Models.CstIcms.IcmsSN,
        _ => Models.CstIcms.Cst90
    };
}
