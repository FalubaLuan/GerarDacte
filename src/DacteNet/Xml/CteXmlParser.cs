using System.Xml.Linq;
using DacteNet.Models;

namespace DacteNet.Xml;

/// <summary>
/// Thrown when the supplied XML is not a CT-e document this library can render (e.g. GTVe, CT-e
/// Simplificado, or malformed XML missing the mandatory infCTe Id/versao attributes - see
/// xml_mapping.md §5 for the two ACBr-equivalent hard-failure points this mirrors).
/// </summary>
public sealed class CteXmlException : Exception
{
    public CteXmlException(string message) : base(message) { }
}

/// <summary>
/// Parses a CT-e XML document (bare &lt;CTe&gt;/&lt;CTeOS&gt;, or enveloped in
/// &lt;cteProc&gt;/&lt;procCTe&gt;/&lt;cteOSProc&gt;/&lt;procCTeOS&gt;) into a <see cref="CteDocument"/>.
///
/// Deliberately built on <see cref="XDocument"/> (a real XML tree) rather than porting ACBr's own
/// substring-search reader (TLeitor) - see xml_mapping.md "Summary for a C# re-implementation" for why.
/// Namespace-agnostic (matches by local element name only), tolerant of either the standard CT-e
/// namespace or none at all.
///
/// Only CT-e (modelo 57, normal/complemento/anulação/substituto/simplificado is NOT included - see
/// below) and CT-e OS (modelo 67) are supported. GTVe (modelo 64) and CT-e Simplificado
/// (tpCTe Simplificado/SubstitutoSimplificado) are out of scope for this library (see docs/limitations.md)
/// and raise <see cref="CteXmlException"/>.
/// </summary>
public static class CteXmlParser
{
    private static readonly string[] EnvelopeTags =
    {
        "cteProc", "procCTe", "cteOSProc", "procCTeOS"
    };

    private static readonly string[] UnsupportedDocTags =
    {
        "GTVe", "GTVeProc", "procGTVe", "CTeSimp", "cteSimpProc", "procCTeSimp"
    };

    public static CteDocument Parse(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            throw new CteXmlException("XML vazio.");

        XDocument doc;
        try
        {
            doc = XDocument.Parse(xml, LoadOptions.None);
        }
        catch (Exception ex)
        {
            throw new CteXmlException($"Falha ao interpretar o XML: {ex.Message}");
        }

        var root = doc.Root ?? throw new CteXmlException("XML sem elemento raiz.");

        if (UnsupportedDocTags.Contains(root.Name.LocalName))
            throw new CteXmlException(
                $"Documento '{root.Name.LocalName}' não é suportado por esta biblioteca (apenas CT-e modelo 57 " +
                "e CT-e OS modelo 67 são suportados; GTVe e CT-e Simplificado estão fora do escopo - ver docs/limitations.md).");

        XElement cteEl;
        XElement? protEl = null;

        if (EnvelopeTags.Contains(root.Name.LocalName))
        {
            cteEl = root.Child("CTe") ?? root.Child("CTeOS")
                ?? throw new CteXmlException($"Envelope '{root.Name.LocalName}' não contém um elemento CTe/CTeOS.");
            protEl = root.Child("protCTe") ?? root.Child("protCTeOS");
        }
        else if (root.Name.LocalName is "CTe" or "CTeOS")
        {
            cteEl = root;
        }
        else
        {
            throw new CteXmlException($"Elemento raiz '{root.Name.LocalName}' não reconhecido como CT-e.");
        }

        var infCte = cteEl.Child("infCte")
            ?? throw new CteXmlException("Elemento infCte não encontrado.");

        var id = infCte.Attr("Id");
        if (string.IsNullOrWhiteSpace(id) || !id.Any(char.IsDigit))
            throw new CteXmlException("Não encontrei o atributo obrigatório: infCTe/@Id");

        var versaoStr = infCte.Attr("versao");
        if (versaoStr is null || !double.TryParse(versaoStr, System.Globalization.CultureInfo.InvariantCulture, out var versao))
            throw new CteXmlException("Não encontrei o atributo obrigatório: infCTe/@versao");

        var cte = new CteDocument { Id = id };

        var ideEl = infCte.Child("ide") ?? throw new CteXmlException("Elemento ide não encontrado.");
        cte.Identificacao = ParseIde(ideEl, versao);

        // tpCTe / modelo sanity: reject CT-e Simplificado / GTVe even if it slipped through under a bare <CTe> root.
        if (cte.Identificacao.TipoCTe is TipoCTe.CTeSimplificado or TipoCTe.SubstitutoCTeSimplificado or TipoCTe.GTVe)
            throw new CteXmlException(
                "CT-e Simplificado e GTVe não são suportados por esta biblioteca (ver docs/limitations.md).");

        cte.Complemento = ParseCompl(infCte.Child("compl"));
        cte.Emitente = ParseEmit(infCte.Child("emit"));

        var isCteOs = cte.Identificacao.Modelo == ModeloDocumento.CTeOS;

        if (versao >= 3.0 || isCteOs)
        {
            var tomaEl = infCte.Child("toma");
            if (tomaEl is not null) cte.Tomador = ParseTomador(tomaEl);
        }

        if (!isCteOs)
        {
            cte.Remetente = ParseRemetente(infCte.Child("rem"));
            cte.Expedidor = ParseExpedidor(infCte.Child("exped"));
            cte.Recebedor = ParseRecebedor(infCte.Child("receb"));
            cte.Destinatario = ParseDestinatario(infCte.Child("dest"));
        }

        cte.ValorPrestacao = ParseVPrest(infCte.Child("vPrest"));

        var impEl = infCte.Child("imp");
        cte.Icms = ParseImp(impEl, versao, out var tributosFederais);
        cte.TributosFederais = tributosFederais;

        var infCteNormEl = infCte.Child("infCTeNorm");
        if (infCteNormEl is not null)
            cte.InfoNormal = ParseInfCTeNorm(infCteNormEl, versao);

        cte.CteComplementado = ParseInfCteComp(infCte, versao);
        cte.CteAnulado = ParseInfCteAnu(infCte.Child("infCTeAnu"));

        var infRespTecEl = infCte.Child("infRespTec");
        if (infRespTecEl is not null)
        {
            cte.ResponsavelTecnico = new ResponsavelTecnico
            {
                Cnpj = infRespTecEl.ChildText("CNPJ"),
                Contato = infRespTecEl.ChildText("xContato"),
                Email = infRespTecEl.ChildText("email"),
                Telefone = infRespTecEl.ChildText("fone"),
            };
        }

        var infCTeSuplEl = cteEl.Child("infCTeSupl");
        if (infCTeSuplEl is not null)
        {
            var qr = infCTeSuplEl.ChildText("qrCodCTe");
            if (qr is not null)
            {
                // ACBr strips a literal CDATA wrapper here (pcteCTeR.pas Ler_InfCTeSupl) - XDocument already
                // resolves CDATA sections to plain text, but some emitters embed the *literal* markers as text.
                qr = qr.Replace("<![CDATA[", "").Replace("]]>", "");
            }
            cte.QrCodeUrl = qr;
        }

        if (protEl is not null)
            cte.Protocolo = ParseProtocolo(protEl);

        return cte;
    }

    private static Identificacao ParseIde(XElement ide, double versao)
    {
        var result = new Identificacao
        {
            Versao = versao,
            CodigoUF = ide.ChildInt("cUF"),
            CodigoCTe = ide.ChildInt("cCT"),
            Cfop = ide.ChildText("CFOP"),
            NaturezaOperacao = ide.ChildText("natOp"),
            Modelo = CteEnumParsers.Modelo(ide.ChildText("mod")),
            Serie = ide.ChildInt("serie"),
            NumeroCTe = ide.ChildInt("nCT"),
            DataHoraEmissao = ide.ChildDateTime("dhEmi"),
            TipoImpressao = CteEnumParsers.TipoImpressao(ide.ChildText("tpImp")),
            TipoEmissao = CteEnumParsers.TipoEmissao(ide.ChildText("tpEmis")),
            DigitoVerificador = ide.ChildInt("cDV"),
            TipoAmbiente = CteEnumParsers.TipoAmbiente(ide.ChildText("tpAmb")),
            TipoCTe = CteEnumParsers.TipoCTe(ide.ChildText("tpCTe")),
            ProcessoEmissao = ide.ChildText("procEmi"),
            VersaoProcesso = ide.ChildText("verProc"),
            ReferenciaCTe = ide.ChildText("refCTe"),
            CodigoMunicipioEnvio = ide.ChildInt("cMunEnv"),
            MunicipioEnvio = ide.ChildText("xMunEnv"),
            UfEnvio = ide.ChildText("UFEnv"),
            Modal = CteEnumParsers.Modal(ide.ChildText("modal")),
            TipoServico = CteEnumParsers.TipoServico(ide.ChildText("tpServ")),
            CodigoMunicipioInicio = ide.ChildInt("cMunIni"),
            MunicipioInicio = ide.ChildText("xMunIni"),
            UfInicio = ide.ChildText("UFIni"),
            CodigoMunicipioFim = ide.ChildInt("cMunFim"),
            MunicipioFim = ide.ChildText("xMunFim"),
            UfFim = ide.ChildText("UFFim"),
            DetalheRetira = ide.ChildText("xDetRetira"),
            DataHoraSaidaOrigem = ide.ChildDateTime("dhSaidaOrig"),
            DataHoraChegadaDestino = ide.ChildDateTime("dhChegadaDest"),
            DataHoraContingencia = ide.ChildDateTime("dhCont"),
            JustificativaContingencia = ide.ChildText("xJust"),
        };

        var retiraStr = ide.ChildText("retira");
        if (retiraStr is not null) result.Retira = CteEnumParsers.Retira(retiraStr);

        if (versao >= 3.0)
            result.IndicadorGlobalizado = CteEnumParsers.IndicadorSimNao(ide.ChildText("indGlobalizado"));

        var indIeTomaStr = ide.ChildText("indIEToma");
        if (indIeTomaStr is not null)
            result.IndicadorIeTomador = CteEnumParsers.IndicadorIeDestinatario(indIeTomaStr);

        foreach (var perc in ide.Children("infPercurso"))
            result.Percurso.Add(new PercursoItem { Uf = perc.ChildText("UFPer") });

        // toma03/toma3/toma - tolerate the three tag-name spellings ACBr itself tolerates (xml_mapping.md §4).
        var toma03El = ide.Child("toma03") ?? ide.Child("toma3") ?? ide.Child("toma");
        if (toma03El is not null && toma03El.ChildText("toma") is { } tomaCode)
            result.Toma03 = new Toma03 { Tomador = CteEnumParsers.Tomador(tomaCode) };

        var toma4El = ide.Child("toma4") ?? ide.Child("tomaTerceiro");
        if (toma4El is not null)
        {
            result.Toma4 = new Toma4
            {
                Tomador = CteEnumParsers.Tomador(toma4El.ChildText("toma")),
                CnpjCpf = toma4El.ChildText("CNPJ") ?? toma4El.ChildText("CPF"),
                InscricaoEstadual = toma4El.ChildText("IE"),
                RazaoSocial = toma4El.ChildText("xNome"),
                NomeFantasia = toma4El.ChildText("xFant"),
                Telefone = toma4El.ChildText("fone"),
                Email = toma4El.ChildText("email"),
                Endereco = ParseEndereco(toma4El.Child("enderToma")),
            };
        }

        return result;
    }

    private static Endereco ParseEndereco(XElement? el) => new()
    {
        Logradouro = el.ChildText("xLgr"),
        Numero = el.ChildText("nro"),
        Complemento = el.ChildText("xCpl"),
        Bairro = el.ChildText("xBairro"),
        CodigoMunicipio = el.ChildInt("cMun"),
        Municipio = el.ChildText("xMun"),
        Cep = el.ChildInt("CEP"),
        Uf = el.ChildText("UF"),
        CodigoPais = el.ChildInt("cPais"),
        Pais = el.ChildText("xPais"),
        Telefone = el.ChildText("fone"),
    };

    private static LocalColetaEntrega ParseLocal(XElement el) => new()
    {
        CnpjCpf = el.ChildText("CNPJ") ?? el.ChildText("CPF"),
        Nome = el.ChildText("xNome"),
        Logradouro = el.ChildText("xLgr"),
        Numero = el.ChildText("nro"),
        Complemento = el.ChildText("xCpl"),
        Bairro = el.ChildText("xBairro"),
        CodigoMunicipio = el.ChildInt("cMun"),
        Municipio = el.ChildText("xMun"),
        Uf = el.ChildText("UF"),
    };

    private static Complemento? ParseCompl(XElement? compl)
    {
        if (compl is null) return null;
        var result = new Complemento
        {
            CaracteristicaAdicional = compl.ChildText("xCaracAd"),
            CaracteristicaServico = compl.ChildText("xCaracSer"),
            DescricaoEmissao = compl.ChildText("xEmi"),
            OrigemCalculo = compl.ChildText("origCalc"),
            DestinoCalculo = compl.ChildText("destCalc"),
            Observacoes = compl.ChildText("xObs"),
        };

        var fluxoEl = compl.Child("fluxo");
        if (fluxoEl is not null)
        {
            result.Fluxo = new FluxoCarga
            {
                Origem = fluxoEl.ChildText("xOrig"),
                Destino = fluxoEl.ChildText("xDest"),
                Rota = fluxoEl.ChildText("xRota"),
            };
            foreach (var p in fluxoEl.Children("pass"))
                if (p.ChildText("xPass") is { } xp) result.Fluxo.Passagens.Add(xp);
        }

        var entregaEl = compl.Child("Entrega");
        if (entregaEl is not null)
        {
            var entrega = new EntregaProgramada { TipoData = TipoDataEntrega.NaoInformado, TipoHora = TipoHoraEntrega.NaoInformado };
            if (entregaEl.Child("semData") is { } semData)
            {
                entrega.TipoData = TipoDataEntrega.SemData;
                entrega.TipoPeriodoTexto = semData.ChildText("tpPer");
            }
            if (entregaEl.Child("comData") is { } comData)
            {
                entrega.TipoData = TipoDataEntrega.NaData;
                entrega.TipoPeriodoTexto = comData.ChildText("tpPer");
                entrega.DataProgramada = comData.ChildDateTime("dProg");
            }
            if (entregaEl.Child("noPeriodo") is { } noPeriodo)
            {
                entrega.TipoData = TipoDataEntrega.NoPeriodo;
                entrega.TipoPeriodoTexto = noPeriodo.ChildText("tpPer");
                entrega.DataInicio = noPeriodo.ChildDateTime("dIni");
                entrega.DataFim = noPeriodo.ChildDateTime("dFim");
            }
            if (entregaEl.Child("semHora") is { } semHora)
            {
                entrega.TipoHora = TipoHoraEntrega.SemHorario;
                entrega.TipoHorarioTexto = semHora.ChildText("tpHor");
            }
            if (entregaEl.Child("comHora") is { } comHora)
            {
                entrega.TipoHora = TipoHoraEntrega.NoHorario;
                entrega.TipoHorarioTexto = comHora.ChildText("tpHor");
                entrega.HoraProgramada = comHora.ChildDateTime("hProg");
            }
            if (entregaEl.Child("noInter") is { } noInter)
            {
                entrega.TipoHora = TipoHoraEntrega.NoIntervalo;
                entrega.TipoHorarioTexto = noInter.ChildText("tpHor");
                entrega.HoraInicio = noInter.ChildDateTime("hIni");
                entrega.HoraFim = noInter.ChildDateTime("hFim");
            }
            result.Entrega = entrega;
        }

        foreach (var oc in compl.Children("ObsCont"))
            result.ObservacoesContribuinte.Add(new ObservacaoItem { Campo = oc.Attr("xCampo"), Texto = oc.ChildText("xTexto") });
        foreach (var of in compl.Children("ObsFisco"))
            result.ObservacoesFisco.Add(new ObservacaoItem { Campo = of.Attr("xCampo"), Texto = of.ChildText("xTexto") });

        return result;
    }

    private static Emitente ParseEmit(XElement? emit)
    {
        if (emit is null) return new Emitente();
        var enderEl = emit.Child("enderEmit");
        return new Emitente
        {
            Cnpj = emit.ChildText("CNPJ"),
            InscricaoEstadual = emit.ChildText("IE"),
            InscricaoEstadualST = emit.ChildText("IEST"),
            RazaoSocial = emit.ChildText("xNome"),
            NomeFantasia = emit.ChildText("xFant"),
            Endereco = ParseEndereco(enderEl),
            Crt = CteEnumParsers.RegimeTributario(emit.ChildText("CRT")),
        };
    }

    private static TomadorServico ParseTomador(XElement toma) => new()
    {
        CnpjCpf = toma.ChildText("CNPJ") ?? toma.ChildText("CPF"),
        InscricaoEstadual = toma.ChildText("IE"),
        RazaoSocial = toma.ChildText("xNome"),
        NomeFantasia = toma.ChildText("xFant"),
        Telefone = toma.ChildText("fone"),
        Email = toma.ChildText("email"),
        InscricaoSuframa = toma.ChildText("ISUF"),
        Endereco = ParseEndereco(toma.Child("enderToma")),
    };

    private static Remetente? ParseRemetente(XElement? rem)
    {
        if (rem is null) return null;
        var result = new Remetente
        {
            CnpjCpf = rem.ChildText("CNPJ") ?? rem.ChildText("CPF"),
            InscricaoEstadual = rem.ChildText("IE"),
            RazaoSocial = rem.ChildText("xNome"),
            NomeFantasia = rem.ChildText("xFant"),
            Telefone = rem.ChildText("fone"),
            Email = rem.ChildText("email"),
            Endereco = ParseEndereco(rem.Child("enderReme")),
        };
        if (rem.Child("locColeta") is { } loc) result.LocalColeta = ParseLocal(loc);
        return result;
    }

    private static Expedidor? ParseExpedidor(XElement? exp)
    {
        if (exp is null) return null;
        return new Expedidor
        {
            CnpjCpf = exp.ChildText("CNPJ") ?? exp.ChildText("CPF"),
            InscricaoEstadual = exp.ChildText("IE"),
            RazaoSocial = exp.ChildText("xNome"),
            Telefone = exp.ChildText("fone"),
            Email = exp.ChildText("email"),
            Endereco = ParseEndereco(exp.Child("enderExped")),
        };
    }

    private static Recebedor? ParseRecebedor(XElement? rec)
    {
        if (rec is null) return null;
        return new Recebedor
        {
            CnpjCpf = rec.ChildText("CNPJ") ?? rec.ChildText("CPF"),
            InscricaoEstadual = rec.ChildText("IE"),
            RazaoSocial = rec.ChildText("xNome"),
            Telefone = rec.ChildText("fone"),
            Email = rec.ChildText("email"),
            Endereco = ParseEndereco(rec.Child("enderReceb")),
        };
    }

    private static Destinatario? ParseDestinatario(XElement? dest)
    {
        if (dest is null) return null;
        var result = new Destinatario
        {
            CnpjCpf = dest.ChildText("CNPJ") ?? dest.ChildText("CPF"),
            InscricaoEstadual = dest.ChildText("IE"),
            InscricaoSuframa = dest.ChildText("ISUF"),
            RazaoSocial = dest.ChildText("xNome"),
            Telefone = dest.ChildText("fone"),
            Email = dest.ChildText("email"),
            Endereco = ParseEndereco(dest.Child("enderDest")),
        };
        if (dest.Child("locEnt") is { } loc) result.LocalEntrega = ParseLocal(loc);
        return result;
    }

    private static ValorPrestacao ParseVPrest(XElement? vPrest)
    {
        var result = new ValorPrestacao
        {
            ValorTotalPrestacao = vPrest.ChildDecimalOrZero("vTPrest"),
            ValorReceber = vPrest.ChildDecimalOrZero("vRec"),
        };
        foreach (var c in vPrest.Children("Comp"))
            result.Componentes.Add(new ComponenteValorPrestacao { Nome = c.ChildText("xNome"), Valor = c.ChildDecimalOrZero("vComp") });
        return result;
    }

    private static Icms ParseImp(XElement? imp, double versao, out InfoTributosFederais? tributosFederais)
    {
        var result = new Icms
        {
            ValorTotalTributos = imp.ChildDecimal("vTotTrib"),
            InformacoesAdicionaisFisco = imp.ChildText("infAdFisco"),
        };
        tributosFederais = null;

        var icmsEl = imp.Child("ICMS");
        if (icmsEl is not null)
        {
            foreach (var groupTag in new[] { "ICMS00", "ICMS20", "ICMS45", "ICMS60", "ICMS90", "ICMSOutraUF", "ICMSSN" })
            {
                var g = icmsEl.Child(groupTag);
                if (g is null) continue;
                var cstRaw = g.ChildText("CST") ?? g.ChildText("CSOSN");
                result.SituacaoTributaria = CteEnumParsers.CstIcms(groupTag, cstRaw);

                switch (groupTag)
                {
                    case "ICMS00":
                        result.BaseCalculo = g.ChildDecimal("vBC");
                        result.AliquotaIcms = g.ChildDecimal("pICMS");
                        result.ValorIcms = g.ChildDecimal("vICMS");
                        break;
                    case "ICMS20":
                        result.PercentualReducaoBaseCalculo = g.ChildDecimal("pRedBC");
                        result.BaseCalculo = g.ChildDecimal("vBC");
                        result.AliquotaIcms = g.ChildDecimal("pICMS");
                        result.ValorIcms = g.ChildDecimal("vICMS");
                        break;
                    case "ICMS45":
                        // CST 40/41/51/45 - isento/não-tributado/diferimento/suspensão: schema carries no vBC/pICMS/vICMS.
                        break;
                    case "ICMS60":
                        result.BaseCalculo = g.ChildDecimal("vBCSTRet");
                        result.AliquotaIcms = g.ChildDecimal("pICMSSTRet");
                        result.ValorIcms = g.ChildDecimal("vICMSSTRet");
                        result.ValorCredito = g.ChildDecimal("vCred");
                        break;
                    case "ICMS90":
                        result.PercentualReducaoBaseCalculo = g.ChildDecimal("pRedBC");
                        result.BaseCalculo = g.ChildDecimal("vBC");
                        result.AliquotaIcms = g.ChildDecimal("pICMS");
                        result.ValorIcms = g.ChildDecimal("vICMS");
                        result.ValorCredito = g.ChildDecimal("vCred");
                        break;
                    case "ICMSOutraUF":
                        result.PercentualReducaoBaseCalculo = g.ChildDecimal("pRedBCOutraUF");
                        result.BaseCalculo = g.ChildDecimal("vBCOutraUF");
                        result.AliquotaIcms = g.ChildDecimal("pICMSOutraUF");
                        result.ValorIcms = g.ChildDecimal("vICMSOutraUF");
                        break;
                    case "ICMSSN":
                        result.IndicadorSimplesNacional = g.ChildInt("indSN") ?? 1;
                        break;
                }
                break; // the XSD choice group means exactly one of these is present
            }
        }

        var ufFimEl = imp.Child("ICMSUFFim");
        if (ufFimEl is not null)
        {
            result.UfFim = new IcmsUfFim
            {
                BaseCalculo = ufFimEl.ChildDecimal("vBCUFFim"),
                PercentualFcp = ufFimEl.ChildDecimal("pFCPUFFim"),
                AliquotaInterna = ufFimEl.ChildDecimal("pICMSUFFim"),
                AliquotaInterestadual = ufFimEl.ChildDecimal("pICMSInter"),
                PercentualPartilha = ufFimEl.ChildDecimal("pICMSInterPart"),
                ValorFcp = ufFimEl.ChildDecimal("vFCPUFFim"),
                ValorIcmsUFFim = ufFimEl.ChildDecimal("vICMSUFFim"),
                ValorIcmsUFIni = ufFimEl.ChildDecimal("vICMSUFIni"),
            };
        }

        var tribFedEl = imp.Child("infTribFed");
        if (tribFedEl is not null)
        {
            tributosFederais = new InfoTributosFederais
            {
                Pis = tribFedEl.ChildDecimal("vPIS"),
                Cofins = tribFedEl.ChildDecimal("vCOFINS"),
                Ir = tribFedEl.ChildDecimal("vIR"),
                Inss = tribFedEl.ChildDecimal("vINSS"),
                Csll = tribFedEl.ChildDecimal("vCSLL"),
            };
        }

        return result;
    }

    private static InfoCTeNormal ParseInfCTeNorm(XElement infCTeNorm, double versao)
    {
        var result = new InfoCTeNormal
        {
            ChaveCteCancelado = infCTeNorm.ChildText("refCTeCanc"),
        };

        var infServicoEl = infCTeNorm.Child("infServico");
        if (infServicoEl is not null)
            result.InformacoesServico = new InformacoesServico
            {
                DescricaoServico = infServicoEl.ChildText("xDescServ"),
                QuantidadeCarga = infServicoEl.ChildDecimal("qCarga"),
            };

        result.InformacoesCarga = ParseInfCarga(infCTeNorm.Child("infCarga"));

        var infDocEl = infCTeNorm.Child("infDoc");
        if (infDocEl is not null)
            result.DocumentosOriginarios = ParseInfDoc(infDocEl);

        var docAntEl = infCTeNorm.Child("docAnt");
        if (docAntEl is not null)
        {
            foreach (var emi in docAntEl.Children("emiDocAnt"))
            {
                var emitAnt = new EmissorDocumentoAnterior
                {
                    CnpjCpf = emi.ChildText("CNPJ") ?? emi.ChildText("CPF"),
                    InscricaoEstadual = emi.ChildText("IE"),
                    Uf = emi.ChildText("UF"),
                    Nome = emi.ChildText("xNome"),
                };
                foreach (var idDocAnt in emi.Children("idDocAnt"))
                {
                    foreach (var p in idDocAnt.Children("idDocAntPap"))
                        emitAnt.DocumentosPapel.Add(new DocumentoAnteriorPapel
                        {
                            Tipo = CteEnumParsers.TipoDocumentoAnteriorPapel(p.ChildText("tpDoc")),
                            Serie = p.ChildText("serie"),
                            SubSerie = p.ChildText("subser"),
                            Numero = p.ChildText("nDoc"),
                            DataEmissao = p.ChildDate("dEmi"),
                        });
                    foreach (var e in idDocAnt.Children("idDocAntEle"))
                        emitAnt.DocumentosEletronicos.Add(new DocumentoAnteriorEletronico
                        {
                            Chave = versao >= 3.0 ? e.ChildText("chCTe") : (e.ChildText("chave") ?? e.ChildText("chCTe")),
                        });
                }
                result.DocumentosAnteriores.Add(emitAnt);
            }
        }

        foreach (var seg in infCTeNorm.Children("seg"))
        {
            result.Seguros.Add(new SeguroCarga
            {
                Responsavel = CteEnumParsers.ResponsavelSeguro(seg.ChildText("respSeg")),
                NomeSeguradora = seg.ChildText("xSeg"),
                NumeroApolice = seg.ChildText("nApol"),
                NumeroAverbacao = seg.ChildText("nAver"),
                ValorCarga = seg.ChildDecimal("vCarga"),
            });
        }

        if (infCTeNorm.Child("infModal").Child("rodo") is { } rodoEl) result.Rodoviario = ParseRodo(rodoEl, versao);
        if (infCTeNorm.Child("rodoOS") is { } rodoOsEl) result.RodoviarioOS = ParseRodoOS(rodoOsEl);
        if (infCTeNorm.Child("aereo") is { } aereoEl) result.Aereo = ParseAereo(aereoEl, versao);
        if (infCTeNorm.Child("aquav") is { } aquavEl) result.Aquaviario = ParseAquav(aquavEl);
        if (infCTeNorm.Child("ferrov") is { } ferrovEl) result.Ferroviario = ParseFerrov(ferrovEl, versao);
        if (infCTeNorm.Child("duto") is { } dutoEl) result.Dutoviario = ParseDuto(dutoEl);
        if (infCTeNorm.Child("multimodal") is { } multiEl) result.Multimodal = ParseMultimodal(multiEl);

        foreach (var peri in infCTeNorm.Children("peri"))
            result.ProdutosPerigosos.Add(ParsePeri(peri));

        foreach (var vn in infCTeNorm.Children("veicNovos"))
            result.VeiculosNovos.Add(new VeiculoNovo
            {
                Chassi = vn.ChildText("chassi"),
                CodigoCor = vn.ChildText("cCor"),
                Cor = vn.ChildText("xCor"),
                CodigoModelo = vn.ChildText("cMod"),
                ValorUnitario = vn.ChildDecimal("vUnit"),
                ValorFrete = vn.ChildDecimal("vFrete"),
            });

        if (infCTeNorm.Child("cobr") is { } cobrEl) result.Cobranca = ParseCobr(cobrEl);

        if (infCTeNorm.Child("infCTeSub") is { } subEl)
        {
            result.CteSubstituto = new InfoCteSubstituto
            {
                ChaveCteSubstituido = subEl.ChildText("chCte"),
                ChaveCteAnulacao = versao >= 3.0 ? subEl.ChildText("refCteAnu") : null,
            };
        }

        var globEl = infCTeNorm.Child("infGlobalizado");
        if (globEl is not null) result.ObservacoesGlobalizado = globEl.ChildText("xObs");

        return result;
    }

    private static InformacoesCarga ParseInfCarga(XElement? infCarga)
    {
        var result = new InformacoesCarga
        {
            ValorCarga = infCarga.ChildDecimal("vCarga"),
            ProdutoPredominante = infCarga.ChildText("proPred"),
            OutrasCaracteristicas = infCarga.ChildText("xOutCat"),
            ValorCargaAverbacao = infCarga.ChildDecimal("vCargaAverb"),
        };
        foreach (var q in infCarga.Children("infQ"))
            result.Quantidades.Add(new QuantidadeCarga
            {
                Unidade = CteEnumParsers.UnidadeMedidaCarga(q.ChildText("cUnid")),
                TipoMedida = q.ChildText("tpMed"),
                Quantidade = q.ChildDecimalOrZero("qCarga"),
            });
        return result;
    }

    private static DocumentosOriginarios ParseInfDoc(XElement infDoc)
    {
        var result = new DocumentosOriginarios();

        foreach (var nf in infDoc.Children("infNF"))
        {
            result.NotasFiscais.Add(new DocumentoNFPapel
            {
                NumeroRoma = nf.ChildText("nRoma"),
                NumeroPedido = nf.ChildText("nPed"),
                Modelo = nf.ChildText("mod"),
                Serie = nf.ChildText("serie"),
                Numero = nf.ChildText("nDoc"),
                DataEmissao = nf.ChildDate("dEmi"),
                BaseCalculo = nf.ChildDecimal("vBC"),
                ValorIcms = nf.ChildDecimal("vICMS"),
                BaseCalculoST = nf.ChildDecimal("vBCST"),
                ValorST = nf.ChildDecimal("vST"),
                ValorProdutos = nf.ChildDecimal("vProd"),
                ValorNF = nf.ChildDecimal("vNF"),
                Cfop = nf.ChildInt("nCFOP"),
                PesoTotal = nf.ChildDecimal("nPeso"),
                Pin = nf.ChildText("PIN"),
                DataPrevista = nf.ChildDate("dPrev"),
            });
        }

        foreach (var nfe in infDoc.Children("infNFe"))
        {
            var chave = nfe.ChildText("chave") ?? nfe.ChildText("chNFe");
            result.NotasFiscaisEletronicas.Add(new DocumentoNFe
            {
                Chave = chave,
                Pin = nfe.ChildText("PIN"),
                DataPrevista = nfe.ChildDate("dPrev"),
            });
        }

        foreach (var outros in infDoc.Children("infOutros"))
        {
            result.Outros.Add(new DocumentoOutros
            {
                Tipo = CteEnumParsers.TipoDocumentoOutros(outros.ChildText("tpDoc")),
                DescricaoOutros = outros.ChildText("descOutros"),
                Numero = outros.ChildText("nDoc"),
                DataEmissao = outros.ChildDate("dEmi"),
                ValorDocumentoFiscal = outros.ChildDecimal("vDocFisc"),
                DataPrevista = outros.ChildDate("dPrev"),
            });
        }

        return result;
    }

    private static ModalRodoviario ParseRodo(XElement rodo, double versao)
    {
        var result = new ModalRodoviario
        {
            Rntrc = rodo.ChildText("RNTRC"),
            DataPrevistaEntrega = rodo.ChildDate("dPrev"),
            Ciot = rodo.ChildText("CIOT"),
        };
        var lotaStr = rodo.ChildText("lota");
        if (lotaStr is not null) result.Lotacao = CteEnumParsers.Lotacao(lotaStr);

        foreach (var occ in rodo.Children("occ"))
        {
            var emiOcc = occ.Child("emiOcc");
            result.Ocorrencias.Add(new OcorrenciaRodoviaria
            {
                Serie = occ.ChildText("serie"),
                Numero = occ.ChildInt("nOcc"),
                DataEmissao = occ.ChildDate("dEmi"),
                EmissorCnpj = emiOcc.ChildText("CNPJ"),
                EmissorUf = emiOcc.ChildText("UF"),
            });
        }
        foreach (var vp in rodo.Children("valePed"))
            result.ValesPedagio.Add(new ValePedagio
            {
                CnpjFornecedora = vp.ChildText("CNPJForn"),
                NumeroCompra = vp.ChildText("nCompra"),
                CnpjPagador = vp.ChildText("CNPJPg"),
                Valor = vp.ChildDecimal("vValePed"),
            });
        foreach (var v in rodo.Children("veic"))
        {
            var prop = v.Child("prop");
            result.Veiculos.Add(new VeiculoRodoviario
            {
                CodigoInterno = v.ChildText("cInt"),
                Renavam = v.ChildText("RENAVAM"),
                Placa = v.ChildText("placa"),
                TaraKg = v.ChildInt("tara"),
                CapacidadeKg = v.ChildInt("capKG"),
                CapacidadeM3 = v.ChildInt("capM3"),
                TipoPropriedade = CteEnumParsers.TipoPropriedadeVeiculo(v.ChildText("tpProp")),
                TipoVeiculo = CteEnumParsers.TipoVeiculo(v.ChildText("tpVeic")),
                TipoRodado = v.ChildText("tpRod"),
                TipoCarroceria = v.ChildText("tpCar"),
                Uf = v.ChildText("UF"),
                Proprietario = prop is null ? null : new ProprietarioVeiculo
                {
                    CnpjCpf = prop.ChildText("CNPJ") ?? prop.ChildText("CPF"),
                    Rntrc = prop.ChildText("RNTRC"),
                    Nome = prop.ChildText("xNome"),
                    InscricaoEstadual = prop.ChildText("IE"),
                    Uf = prop.ChildText("UF"),
                },
            });
        }
        foreach (var l in rodo.Children("lacRodo"))
            result.Lacres.Add(new LacreItem { Numero = l.ChildText("nLacre") });
        foreach (var m in rodo.Children("moto"))
            result.Motoristas.Add(new MotoristaItem { Nome = m.ChildText("xNome"), Cpf = m.ChildText("CPF") });

        return result;
    }

    private static ModalRodoviarioOS ParseRodoOS(XElement rodoOs)
    {
        var veicEl = rodoOs.Child("veic");
        VeiculoRodoviarioOS? veic = null;
        if (veicEl is not null)
        {
            var prop = veicEl.Child("prop");
            veic = new VeiculoRodoviarioOS
            {
                Placa = veicEl.ChildText("placa"),
                Renavam = veicEl.ChildText("RENAVAM"),
                Uf = veicEl.ChildText("UF"),
                Proprietario = prop is null ? null : new ProprietarioVeiculoOS
                {
                    CnpjCpf = prop.ChildText("CNPJ") ?? prop.ChildText("CPF"),
                    Taf = prop.ChildText("TAF"),
                    NumeroRegistroEstadual = prop.ChildText("NroRegEstadual"),
                    Nome = prop.ChildText("xNome"),
                    InscricaoEstadual = prop.ChildText("IE"),
                    Uf = prop.ChildText("UF"),
                },
            };
        }
        var fretEl = rodoOs.Child("infFretamento");
        return new ModalRodoviarioOS
        {
            Taf = rodoOs.ChildText("TAF"),
            NumeroRegistroEstadual = rodoOs.ChildText("NroRegEstadual"),
            Veiculo = veic,
            TipoFretamento = fretEl is null ? null : CteEnumParsers.TipoFretamento(fretEl.ChildText("tpFretamento")),
            DataHoraViagem = fretEl?.ChildDateTime("dhViagem"),
        };
    }

    private static ModalAereo ParseAereo(XElement aereo, double versao)
    {
        var tarifaEl = aereo.Child("tarifa");
        var natCargaEl = aereo.Child("natCarga");
        var result = new ModalAereo
        {
            NumeroMinuta = aereo.ChildInt("nMinu"),
            NumeroOCA = aereo.ChildText("nOCA"),
            DataPrevistaEntrega = aereo.ChildDate("dPrevAereo"),
            LocalAgenciaEmissao = aereo.ChildText("xLAgEmi"),
            IdentificacaoTerminal = aereo.ChildText("IdT"),
            Tarifa = tarifaEl is null ? null : new TarifaAerea
            {
                Classe = tarifaEl.ChildText("CL"),
                Codigo = tarifaEl.ChildText("cTar"),
                Valor = tarifaEl.ChildDecimal("vTar"),
            },
            Dimensoes = natCargaEl.ChildText("xDime"),
            InformacoesComplementaresManuseio = natCargaEl.ChildText("cIMP"),
        };
        if (natCargaEl is not null)
            foreach (var m in natCargaEl.Children("cinfManu"))
                if (m.ChildText("nInfManu") is { } code) result.InstrucoesManuseio.Add(code);
        return result;
    }

    private static ModalAquaviario ParseAquav(XElement aquav)
    {
        var result = new ModalAquaviario
        {
            ValorPrestacao = aquav.ChildDecimal("vPrest"),
            ValorAfrmm = aquav.ChildDecimal("vAFRMM"),
            NumeroBooking = aquav.ChildText("nBooking"),
            NumeroControle = aquav.ChildText("nCtrl"),
            NomeNavio = aquav.ChildText("xNavio"),
            NumeroViagem = aquav.ChildText("nViag"),
            Direcao = CteEnumParsers.Direcao(aquav.ChildText("direc")),
            PortoEmbarque = aquav.ChildText("prtEmb"),
            PortoTransbordo = aquav.ChildText("prtTrans"),
            PortoDestino = aquav.ChildText("prtDest"),
            TipoNavegacao = CteEnumParsers.TipoNavegacao(aquav.ChildText("tpNav")),
            Irin = aquav.ChildText("irin"),
        };
        foreach (var b in aquav.Children("balsa"))
            if (b.ChildText("xBalsa") is { } xb) result.Balsas.Add(xb);
        foreach (var dc in aquav.Children("detCont"))
        {
            var cont = new ContainerAquaviario { Numero = dc.ChildText("nCont") };
            foreach (var l in dc.Children("Lacre"))
                cont.Lacres.Add(new LacreItem { Numero = l.ChildText("nLacre") });
            result.Containeres.Add(cont);
        }
        return result;
    }

    private static ModalFerroviario ParseFerrov(XElement ferrov, double versao)
    {
        var result = new ModalFerroviario
        {
            TipoTrafego = CteEnumParsers.TipoTrafego(ferrov.ChildText("tpTraf")),
            Fluxo = ferrov.ChildText("fluxo"),
            IdentificacaoTrem = ferrov.ChildText("idTrem"),
        };

        if (versao >= 3.0)
        {
            var trafMutEl = ferrov.Child("trafMut");
            result.ValorFrete = trafMutEl.ChildDecimal("vFrete");
            result.ChaveCteFerroOrigem = trafMutEl.ChildText("chCTeFerroOrigem");
        }
        else
        {
            result.ValorFrete = ferrov.ChildDecimal("vFrete");
            foreach (var v in ferrov.Children("detVag"))
                result.Vagoes.Add(new VagaoFerroviario
                {
                    Numero = v.ChildInt("nVag"),
                    Capacidade = v.ChildDecimal("cap"),
                    Tipo = v.ChildText("tpVag"),
                    PesoReal = v.ChildDecimal("pesoR"),
                    PesoBaseCalculo = v.ChildDecimal("pesoBC"),
                });
        }
        return result;
    }

    private static ModalDutoviario ParseDuto(XElement duto) => new()
    {
        ValorTarifa = duto.ChildDecimal("vTar"),
        DataInicio = duto.ChildDate("dIni"),
        DataFim = duto.ChildDate("dFim"),
        Classe = CteEnumParsers.ClasseDuto(duto.ChildText("classDuto")),
    };

    private static ModalMultimodal ParseMultimodal(XElement multi) => new()
    {
        CertificadoOperador = multi.ChildText("COTM"),
        Negociavel = multi.ChildText("indNegociavel") == "1",
        NomeSeguradora = multi.ChildText("xSeg"),
        CnpjSeguradora = multi.ChildText("CNPJ"),
        NumeroApolice = multi.ChildText("nApol"),
        NumeroAverbacao = multi.ChildText("nAver"),
    };

    private static ProdutoPerigoso ParsePeri(XElement peri) => new()
    {
        NumeroOnu = peri.ChildText("nONU"),
        NomeApropriado = peri.ChildText("xNomeAE"),
        ClasseRisco = peri.ChildText("xClaRisco"),
        GrupoEmbalagem = peri.ChildText("grEmb"),
        QuantidadeTotalProduto = peri.ChildText("qTotProd"),
        QuantidadeVolumoTipo = peri.ChildText("qVolTipo"),
        PontoFulgor = peri.ChildText("pontoFulgor"),
        QuantidadeTotalEmbalagem = peri.ChildText("qTotEmb"),
    };

    private static Cobranca ParseCobr(XElement cobr)
    {
        var fatEl = cobr.Child("fat");
        var result = new Cobranca
        {
            NumeroFatura = fatEl.ChildText("nFat"),
            ValorOriginal = fatEl.ChildDecimal("vOrig"),
            ValorDesconto = fatEl.ChildDecimal("vDesc"),
            ValorLiquido = fatEl.ChildDecimal("vLiq"),
        };
        foreach (var d in cobr.Children("dup"))
            result.Duplicatas.Add(new DuplicataItem
            {
                Numero = d.ChildText("nDup"),
                Vencimento = d.ChildDate("dVenc"),
                Valor = d.ChildDecimal("vDup"),
            });
        return result;
    }

    private static InfoCteComplementado? ParseInfCteComp(XElement infCte, double versao)
    {
        if (versao > 3.0)
        {
            var chaves = infCte.Children("infCteComp").Select(e => e.ChildText("chCTe")).Where(s => s is not null).Cast<string>().ToList();
            return chaves.Count > 0 ? new InfoCteComplementado { ChavesComplementadas = chaves } : null;
        }
        var single = infCte.Child("infCteComp");
        if (single is null) return null;
        var chave = versao >= 3.0 ? single.ChildText("chCTe") : single.ChildText("chave");
        return chave is null ? null : new InfoCteComplementado { ChavesComplementadas = { chave } };
    }

    private static InfoCteAnulado? ParseInfCteAnu(XElement? el)
    {
        if (el is null) return null;
        return new InfoCteAnulado { Chave = el.ChildText("chCTe") ?? el.ChildText("chCte"), DataEmissao = el.ChildDate("dEmi") };
    }

    private static ProtocoloAutorizacao ParseProtocolo(XElement protEl)
    {
        var infProt = protEl.Child("infProt") ?? protEl;
        return new ProtocoloAutorizacao
        {
            TipoAmbiente = CteEnumParsers.TipoAmbiente(infProt.ChildText("tpAmb")),
            VersaoAplicativo = infProt.ChildText("verAplic"),
            ChaveAcesso = infProt.ChildText("chCTe"),
            DataHoraRecebimento = infProt.ChildDateTime("dhRecbto"),
            NumeroProtocolo = infProt.ChildText("nProt"),
            CodigoStatus = infProt.ChildInt("cStat"),
            MotivoStatus = infProt.ChildText("xMotivo"),
        };
    }
}
