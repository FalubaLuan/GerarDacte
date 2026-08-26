namespace DacteNet.Models;

// Enum names and value sets below are ported from ACBrCTe's pcteConversaoCTe.pas / ACBrCTe.Conversao.pas
// and from the official CT-e XSD (www.portalfiscal.inf.br/cte) wherever the ACBr source itself did not
// carry the full value set (see /home/claude/work/analysis/cte_model.md §3.2 for what was and was not
// directly verifiable inside the provided ACBr source tree).

public enum TipoAmbiente
{
    Producao = 1,
    Homologacao = 2
}

public enum TipoEmissao
{
    Normal = 1,
    Contingencia = 2,
    SCAN = 3,
    DPEC = 4,
    FSDA = 5,
    SVCAN = 6,
    SVCRS = 7,
    SVCSP = 8,
    OffLine = 9
}

public enum ModeloDocumento
{
    CTe = 57,
    GTVe = 64,
    CTeOS = 67
}

public enum TipoCTe
{
    Normal = 0,
    Complemento = 1,
    Anulacao = 2,
    Substituto = 3,
    GTVe = 4,
    CTeSimplificado = 5,
    SubstitutoCTeSimplificado = 6
}

public enum TipoServico
{
    Normal = 0,
    Subcontratacao = 1,
    Redespacho = 2,
    RedespachoIntermediario = 3,
    Multimodal = 4,
    TransportePessoas = 6,
    TransporteValores = 7,
    ExcessoBagagem = 8,
    GTV = 9
}

public enum Modal
{
    Rodoviario = 1,
    Aereo = 2,
    Aquaviario = 3,
    Ferroviario = 4,
    Dutoviario = 5,
    Multimodal = 6
}

public enum TipoImpressao
{
    Retrato = 1,
    Paisagem = 2
}

public enum TamanhoPapel
{
    A4,
    A5
}

public enum Tomador
{
    Remetente = 0,
    Expedidor = 1,
    Recebedor = 2,
    Destinatario = 3,
    Outros = 4
}

public enum IndicadorSimNao
{
    Nao = 0,
    Sim = 1
}

public enum IndicadorIeDestinatario
{
    ContribuinteICMS = 1,
    ContribuinteIsento = 2,
    NaoContribuinte = 9
}

public enum Retira
{
    Sim = 0,
    Nao = 1
}

public enum CstIcms
{
    Cst00 = 0,
    Cst20 = 20,
    Cst40 = 40,
    Cst41 = 41,
    Cst45 = 45,
    Cst51 = 51,
    Cst60 = 60,
    Cst90 = 90,
    IcmsOutraUF = 900, // synthetic discriminator: partilha interestadual (ICMSUFFim), CST field itself is '90'
    IcmsSN = 901        // synthetic discriminator: Simples Nacional
}

public enum RegimeTributario
{
    Nenhum = 0,
    SimplesNacional = 1,
    SimplesNacionalExcessoReceita = 2,
    RegimeNormal = 3,
    SimplesNacionalMEI = 4
}

public enum ResponsavelSeguro
{
    Remetente = 0,
    Expedidor = 1,
    Recebedor = 2,
    Destinatario = 3,
    EmitenteCTe = 4,
    TomadorServico = 5
}

public enum UnidadeMedidaCarga
{
    Kg = 0,
    Ton = 1,
    Litros = 2,
    MMBTU = 3,
    Unidade = 4,
    M3 = 5,
    Desconhecida = -1
}

public enum TipoDocumentoOutros
{
    Declaracao,
    Dutoviario,
    CFeSAT,
    NFCe,
    Outros,
    NaoInformado
}

public enum TipoDocumentoAnteriorPapel
{
    CTRC, CTAC, ACT, NF7, NF27, CAN, CTMC, ATRE, DTA, CAI, CCPI, CA, TIF, BL, Outros
}

public enum TipoVeiculo
{
    Tracao = 0,
    Reboque = 1
}

public enum TipoPropriedadeVeiculo
{
    Proprio,
    Terceiro
}

public enum Lotacao
{
    Nao = 0,
    Sim = 1
}

public enum TipoNavegacao
{
    Interior,
    Cabotagem
}

public enum DirecaoAquaviaria
{
    Norte,
    Leste,
    Sul,
    Oeste
}

public enum TipoTrafegoFerroviario
{
    Proprio = 0,
    Mutuo = 1,
    Rodoferroviario = 2,
    Rodoviario = 3
}

public enum ClasseDuto
{
    Nenhum,
    Gasoduto,
    Mineroduto,
    Oleoduto
}

public enum TipoDataEntrega
{
    SemData,
    NaData,
    AteData,
    APartirData,
    NoPeriodo,
    NaoInformado
}

public enum TipoHoraEntrega
{
    SemHorario,
    NoHorario,
    AteHorario,
    APartirHorario,
    NoIntervalo,
    NaoInformado
}

public enum TipoFretamento
{
    Nenhum,
    Eventual,
    Continuo
}
