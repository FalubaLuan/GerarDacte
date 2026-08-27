using DacteNet.Models;

namespace DacteNet.ViewModel;

/// <summary>
/// Maps a parsed <see cref="CteDocument"/> (+ print-time <see cref="DacteOptions"/>) into the flattened,
/// print-ready <see cref="DacteViewModel"/> that the A4/A5 renderers read from. This is the "business
/// rules" layer: every decision here (which field wins, how a value is formatted, when a block is shown)
/// is taken from the rules extracted into analysis/retrato_layout.md §4 (conditional visibility) and §5
/// (calculations/formatting), analysis/fastreport_crosscheck.md §2 (independent cross-check, used
/// especially for the Tomador resolution and canhoto-summary formulas, which are quoted almost verbatim
/// from both files), and the exact `GetTextoResumoCanhoto` quoted from
/// ACBrCTe/DACTE/Fortes/ACBrCTeDACTeRL.pas.
///
/// Only CT-e (modelo 57) and CT-e OS (modelo 67), tpCTe in [Normal, Complemento, Anulacao, Substituto],
/// are handled - GTVe and CT-e Simplificado are rejected earlier, by <see cref="Xml.CteXmlParser"/>.
///
/// Known simplifications/inferences (see docs/limitations.md for the full list once written):
///  - "Documentos originários" CNPJ/CPF column: ACBr's FastReport variant (`CarregaDadosNotasFiscais`)
///    shows the Remetente's CNPJ/CPF for every paper-NF/NFe/Outros row; the RL variant's own `Itens`
///    procedure is only described narratively in retrato_layout.md §7 (not quoted verbatim), so that
///    same Remetente-CNPJ convention is used here for parity across engines - flagged as an inference.
///  - Legacy (&lt;3.00) "forma de pagamento" (ide/forPag) is not modeled (CteDocument has no such field -
///    an intentional scope simplification, since it's an obsolete field on modern CT-e's) - the header
///    "FORMA DE PAGAMENTO"/"INFORMAÇÕES DO CT-E GLOBALIZADO" slot is therefore only ever populated with
///    the v&gt;=3.00 CT-e Globalizado observation text.
///  - True-contingency mode (`GerarChaveContingencia`) cannot be reproduced - see docs/limitations.md;
///    <see cref="DacteViewModel.UsarBarcodeContingencia"/> is still set so a renderer/caller can detect
///    the situation, but <see cref="DacteViewModel.BarcodeDigitos"/>/<see cref="DacteViewModel.TextoProtocolo"/>
///    are left blank rather than inventing a key.
///  - CST description text (`CSTICMSToStrTagPosText`) and address-mask patterns (`FormatarCNPJ` etc. -
///    already implemented faithfully in <see cref="Format"/>) live outside the two analyzed DACTE files;
///    the CST descriptions below are standard SEFAZ CST-table wording, not a verified ACBr string.
/// </summary>
public static class DacteViewModelBuilder
{
    public static DacteViewModel Build(CteDocument cte, DacteOptions? options = null)
    {
        options ??= new DacteOptions();
        var ide = cte.Identificacao;
        bool isOS = ide.Modelo == ModeloDocumento.CTeOS;
        double versao = ide.Versao;

        var vm = new DacteViewModel
        {
            ModeloOS = isOS,
            CteSimplificado = false,
            TituloDacte = isOS ? "DACTE OS" : "DACTE",
            SubtituloDacte = "Documento Auxiliar do Conhecimento de Transporte Eletrônico" + (isOS ? " para Outros Serviços" : ""),
            RotuloCTe = isOS ? "CT-E OS" : "CT-E",
            Modal = ModalTexto(ide.Modal),
            NumeroSerie = $"{ide.Serie}/{ide.NumeroCTe}",
            NumeroCTe = Format.NumeroDocumentoFiscal(ide.NumeroCTe),
            Serie = ide.Serie?.ToString() ?? "",
            ChaveAcessoFormatada = Format.ChaveAcesso(cte.ChaveAcesso),
            ChaveAcessoDigitos = cte.ChaveAcesso,
            DataHoraEmissao = Format.DataHoraBr(ide.DataHoraEmissao),
            NaturezaOperacao = ide.NaturezaOperacao ?? "",
            Cfop = (ide.Cfop ?? "").PadLeft(4, '0'),
            MunicipioInicio = FormatMunicipioCompleto(ide.MunicipioInicio, ide.UfInicio, ide.CodigoMunicipioInicio),
            MunicipioFim = FormatMunicipioCompleto(ide.MunicipioFim, ide.UfFim, ide.CodigoMunicipioFim),
            EmitenteSite = string.IsNullOrWhiteSpace(options.Site) ? null : options.Site,
            EmitenteEmail = string.IsNullOrWhiteSpace(options.Email) ? null : options.Email,
        };

        BuildTomadorHeaderIndicator(vm, cte, versao, isOS);
        vm.Emitente = BuildEnderecoEmitente(cte.Emitente, options);

        var tomadorResolved = ResolveTomador(cte);
        vm.TomadorServico = tomadorResolved;
        vm.Remetente = cte.Remetente is null ? null : BuildEnderecoRemetente(cte.Remetente);
        vm.Expedidor = cte.Expedidor is null ? null : BuildEnderecoExpedidor(cte.Expedidor);
        vm.Recebedor = cte.Recebedor is null ? null : BuildEnderecoRecebedor(cte.Recebedor);
        vm.Destinatario = cte.Destinatario is null ? null : BuildEnderecoDestinatario(cte.Destinatario);

        BuildStatusWatermarkAndProtocolo(vm, cte, options);

        vm.MostrarQrCode = !string.IsNullOrWhiteSpace(cte.QrCodeUrl);
        vm.QrCodeUrl = cte.QrCodeUrl;

        var infNormal = cte.InfoNormal;
        var infCarga = infNormal?.InformacoesCarga ?? new InformacoesCarga();

        vm.ProdutoPredominante = infCarga.ProdutoPredominante ?? "";
        vm.OutrasCaracteristicasCarga = infCarga.OutrasCaracteristicas ?? "";
        vm.ValorTotalCarga = Format.Moeda(infCarga.ValorCarga);

        BuildPesoCubagemVolumes(vm, infCarga);
        BuildSeguro(vm, infNormal, ide, isOS);
        BuildValoresEComponentes(vm, cte);
        BuildIcms(vm, cte, versao, isOS);

        if (infNormal is not null)
        {
            vm.ProdutosPerigosos = infNormal.ProdutosPerigosos.Select(p => new ProdutoPerigosoVm
            {
                NumeroOnu = p.NumeroOnu ?? "",
                NomeApropriado = p.NomeApropriado ?? "",
                ClasseRisco = p.ClasseRisco ?? "",
                GrupoEmbalagem = p.GrupoEmbalagem ?? "",
                Quantidade = p.QuantidadeTotalProduto ?? "",
            }).ToList();

            vm.VeiculosNovos = infNormal.VeiculosNovos.Select(v => new VeiculoNovoVm
            {
                Chassi = v.Chassi ?? "",
                Cor = v.Cor ?? "",
                Modelo = v.CodigoModelo ?? "",
                ValorUnitario = Format.Moeda(v.ValorUnitario),
                ValorFrete = Format.Moeda(v.ValorFrete),
            }).ToList();

            if (infNormal.CteSubstituto is not null && !string.IsNullOrWhiteSpace(infNormal.ObservacoesGlobalizado))
            {
                // handled below in BuildTomadorHeaderIndicator via infGlobalizado xObs
            }
        }

        BuildDocumentosOriginarios(vm, infNormal, isOS);
        BuildComplementoESubstituicao(vm, cte, ide, isOS);
        BuildModais(vm, infNormal, ide, versao, isOS);
        BuildObservacoesEFisco(vm, cte);
        BuildInformacoesServicoOS(vm, infNormal, isOS);

        vm.RodapeSistema = string.IsNullOrWhiteSpace(options.Sistema) ? null : options.Sistema;
        vm.RodapeUsuario = string.IsNullOrWhiteSpace(options.Usuario) ? null : options.Usuario;
        vm.RodapeDataHoraImpressao = options.ImprimirHoraSaida
            ? (string.IsNullOrWhiteSpace(options.ImprimirHoraSaidaHora) ? Format.DataHoraBr(DateTimeOffset.Now) : options.ImprimirHoraSaidaHora)
            : null;

        vm.TextoResumoCanhoto = options.ExibirResumoCanhoto ? BuildTextoResumoCanhoto(cte) : null;

        return vm;
    }

    // ------------------------------------------------------------------
    // Header: modal/toma indicator toggle (cabecalhoVersao30, retrato_layout.md line 236-239)
    // ------------------------------------------------------------------

    private static void BuildTomadorHeaderIndicator(DacteViewModel vm, CteDocument cte, double versao, bool isOS)
    {
        var ide = cte.Identificacao;

        // rllTipoCte/rllTipoServico (4,137,168,15 / 178,137,132,15): static, non-toggling, always
        // populated regardless of versão/modelo - tpCTToStrText/TpServToStrText, quoted verbatim
        // (modulo accents) from ACBrCTe.Conversao.pas.
        vm.TipoCteTexto = TipoCteToStrText(ide.TipoCTe);
        vm.TipoServicoTexto = TipoServicoToStrText(ide.TipoServico);
        // A5's rllTomaServico always shows this (no version/globalizado branch there) - see the field's own doc comment.
        vm.TomadorDescricaoTexto = TomadorTexto(ide);

        if (isOS)
        {
            // modelo=67 (cabecalhoVersao30): RLLabel28's slot shows CFOP/natOp instead of the
            // globalizado indicator, and RLLabel78's slot (forma de pagamento / informações
            // globalizado) is blank - retrato_layout.md lines 236/238.
            vm.RotuloTomaServicoIndicador = "CÓDIGO FISCAL DE OPERAÇÕES E PRESTAÇÕES - NATUREZA DA OPERAÇÃO";
            vm.TomaServicoIndicador = $"{(ide.Cfop ?? "").PadLeft(4, '0')} - {ide.NaturezaOperacao}";
            vm.RotuloFormaPagamento = "";
            vm.ObservacaoFormaPagamento = "";
            return;
        }

        if (versao >= 3.00)
        {
            vm.RotuloTomaServicoIndicador = "INDICADOR DO CT-E GLOBALIZADO";
            vm.TomaServicoIndicador = ide.IndicadorGlobalizado == IndicadorSimNao.Sim ? "SIM" : "NÃO";
            vm.RotuloFormaPagamento = "INFORMAÇÕES DO CT-E GLOBALIZADO";
            vm.ObservacaoFormaPagamento = cte.InfoNormal?.ObservacoesGlobalizado ?? "";
        }
        else
        {
            // v<3.00: RLLabel28/RLLabel78 stay at their static "TOMADOR DO SERVIÇO"/"FORMA DE
            // PAGAMENTO" captions (DacteViewModel's own defaults). This slot shows a description of
            // who the tomador is (TpTomadorToStrText).
            vm.TomaServicoIndicador = TomadorTexto(ide);
            // ide/forPag is not modeled (see class remarks) - left blank.
            vm.ObservacaoFormaPagamento = "";
        }
    }

    // tpCTToStrText - ACBrCTe.Conversao.pas (also duplicated verbatim in pcteConversaoCTe.pas)
    private static string TipoCteToStrText(TipoCTe? t) => t switch
    {
        TipoCTe.Normal => "NORMAL",
        TipoCTe.Complemento => "COMPLEMENTO",
        TipoCTe.Anulacao => "ANULAÇÃO",
        TipoCTe.Substituto => "SUBSTITUTO",
        TipoCTe.GTVe => "GTVe",
        TipoCTe.CTeSimplificado => "CTe Simplificado",
        TipoCTe.SubstitutoCTeSimplificado => "CTe Simplificado Substituto",
        _ => "",
    };

    // TpServToStrText - ACBrCTe.Conversao.pas
    private static string TipoServicoToStrText(TipoServico? t) => t switch
    {
        TipoServico.Normal => "NORMAL",
        TipoServico.Subcontratacao => "SUBCONTRATAÇÃO",
        TipoServico.Redespacho => "REDESPACHO",
        TipoServico.RedespachoIntermediario => "REDESP. INTERMEDIÁRIO",
        TipoServico.Multimodal => "VINC. A MULTIMODAL",
        TipoServico.TransportePessoas => "TRANSP. PESSOAS",
        TipoServico.TransporteValores => "TRANSP. VALORES",
        TipoServico.ExcessoBagagem => "EXCESSO BAGAGEM",
        TipoServico.GTV => "GTV",
        _ => "",
    };

    private static string TomadorTexto(Identificacao ide)
    {
        if (!string.IsNullOrWhiteSpace(ide.Toma4?.RazaoSocial)) return "OUTROS";
        return ide.Toma03?.Tomador switch
        {
            Tomador.Remetente => "REMETENTE",
            Tomador.Expedidor => "EXPEDIDOR",
            Tomador.Recebedor => "RECEBEDOR",
            Tomador.Destinatario => "DESTINATÁRIO",
            Tomador.Outros => "OUTROS",
            _ => "",
        };
    }

    // ------------------------------------------------------------------
    // Tomador resolution (CarregaTomador, fastreport_crosscheck.md §2.6)
    // ------------------------------------------------------------------

    private static EnderecoVm? ResolveTomador(CteDocument cte)
    {
        var ide = cte.Identificacao;
        bool modeloComToma = ide.Modelo == ModeloDocumento.CTe || ide.Modelo == ModeloDocumento.CTeOS;
        if (modeloComToma && cte.Tomador is not null && !string.IsNullOrWhiteSpace(cte.Tomador.RazaoSocial))
        {
            var t = cte.Tomador;
            return new EnderecoVm
            {
                RazaoSocial = t.RazaoSocial ?? "",
                CnpjCpf = Format.CnpjOuCpf(t.CnpjCpf),
                InscricaoEstadual = t.InscricaoEstadual ?? "",
                EnderecoLinha = FormatLogradouro(t.Endereco),
                Bairro = t.Endereco.Bairro ?? "",
                MunicipioUf = FormatMunicipioUf(t.Endereco.Municipio, t.Endereco.Uf),
                Cep = Format.Cep(t.Endereco.Cep),
                Fone = Format.Fone(t.Telefone),
                Pais = t.Endereco.Pais ?? "",
            };
        }

        if (!string.IsNullOrWhiteSpace(ide.Toma4?.RazaoSocial))
        {
            var t4 = ide.Toma4!;
            return new EnderecoVm
            {
                RazaoSocial = t4.RazaoSocial ?? "",
                CnpjCpf = Format.CnpjOuCpf(t4.CnpjCpf),
                InscricaoEstadual = t4.InscricaoEstadual ?? "",
                EnderecoLinha = FormatLogradouro(t4.Endereco),
                Bairro = t4.Endereco.Bairro ?? "",
                MunicipioUf = FormatMunicipioUf(t4.Endereco.Municipio, t4.Endereco.Uf),
                Cep = Format.Cep(t4.Endereco.Cep),
                Fone = Format.Fone(t4.Telefone),
                Pais = t4.Endereco.Pais ?? "",
            };
        }

        return ide.Toma03?.Tomador switch
        {
            Tomador.Remetente => cte.Remetente is null ? null : BuildEnderecoRemetente(cte.Remetente),
            Tomador.Expedidor => cte.Expedidor is null ? null : BuildEnderecoExpedidor(cte.Expedidor),
            Tomador.Recebedor => cte.Recebedor is null ? null : BuildEnderecoRecebedor(cte.Recebedor),
            Tomador.Destinatario => cte.Destinatario is null ? null : BuildEnderecoDestinatario(cte.Destinatario),
            _ => null,
        };
    }

    // ------------------------------------------------------------------
    // Party -> EnderecoVm builders
    // ------------------------------------------------------------------

    /// <summary>
    /// Builds the header issuer address block, mirroring rlmDadosEmitente's exact line-by-line
    /// composition quoted in retrato_layout.md (rlb_02_Cabecalho): logradouro+número, complemento,
    /// bairro, "CEP: x - município - UF", "CNPJ: x", "INSCRIÇÃO ESTADUAL: x", "TELEFONE: x", then
    /// optional SITE:/E-MAIL: lines sourced from <see cref="DacteOptions"/> (not the XML).
    /// </summary>
    private static EnderecoVm BuildEnderecoEmitente(Emitente e, DacteOptions? options = null)
    {
        var end = e.Endereco;
        var linhas = new List<string>();
        var linha1 = $"{end.Logradouro}, {end.Numero}".Trim(' ', ',');
        if (!string.IsNullOrWhiteSpace(linha1)) linhas.Add(linha1);
        if (!string.IsNullOrWhiteSpace(end.Complemento)) linhas.Add(end.Complemento!);
        if (!string.IsNullOrWhiteSpace(end.Bairro)) linhas.Add(end.Bairro!);
        linhas.Add($"CEP: {Format.Cep(end.Cep)} - {end.Municipio} - {end.Uf}");
        linhas.Add($"CNPJ: {Format.CnpjOuCpf(e.Cnpj)}");
        if (!string.IsNullOrWhiteSpace(e.InscricaoEstadual)) linhas.Add($"INSCRIÇÃO ESTADUAL: {e.InscricaoEstadual}");
        if (!string.IsNullOrWhiteSpace(end.Telefone)) linhas.Add($"TELEFONE: {Format.Fone(end.Telefone)}");
        if (!string.IsNullOrWhiteSpace(options?.Site)) linhas.Add($"SITE: {options!.Site}");
        if (!string.IsNullOrWhiteSpace(options?.Email)) linhas.Add($"E-MAIL: {options!.Email}");

        return new EnderecoVm
        {
            RazaoSocial = e.RazaoSocial ?? "",
            CnpjCpf = Format.CnpjOuCpf(e.Cnpj),
            InscricaoEstadual = e.InscricaoEstadual ?? "",
            EnderecoLinha = FormatLogradouro(e.Endereco),
            Bairro = e.Endereco.Bairro ?? "",
            MunicipioUf = FormatMunicipioUf(e.Endereco.Municipio, e.Endereco.Uf),
            Cep = Format.Cep(e.Endereco.Cep),
            Fone = Format.Fone(e.Endereco.Telefone),
            Pais = e.Endereco.Pais ?? "",
            LinhasEnderecoCompleto = linhas,
        };
    }

    private static EnderecoVm BuildEnderecoRemetente(Remetente r) => new()
    {
        RazaoSocial = r.RazaoSocial ?? "",
        CnpjCpf = Format.CnpjOuCpf(r.CnpjCpf),
        InscricaoEstadual = r.InscricaoEstadual ?? "",
        EnderecoLinha = FormatLogradouro(r.Endereco),
        Bairro = r.Endereco.Bairro ?? "",
        MunicipioUf = FormatMunicipioUf(r.Endereco.Municipio, r.Endereco.Uf),
        Cep = Format.Cep(r.Endereco.Cep),
        Fone = Format.Fone(r.Telefone),
        Pais = r.Endereco.Pais ?? "",
    };

    private static EnderecoVm BuildEnderecoExpedidor(Expedidor x) => new()
    {
        RazaoSocial = x.RazaoSocial ?? "",
        CnpjCpf = Format.CnpjOuCpf(x.CnpjCpf),
        InscricaoEstadual = x.InscricaoEstadual ?? "",
        EnderecoLinha = FormatLogradouro(x.Endereco),
        Bairro = x.Endereco.Bairro ?? "",
        MunicipioUf = FormatMunicipioUf(x.Endereco.Municipio, x.Endereco.Uf),
        Cep = Format.Cep(x.Endereco.Cep),
        Fone = Format.Fone(x.Telefone),
        Pais = x.Endereco.Pais ?? "",
    };

    private static EnderecoVm BuildEnderecoRecebedor(Recebedor r) => new()
    {
        RazaoSocial = r.RazaoSocial ?? "",
        CnpjCpf = Format.CnpjOuCpf(r.CnpjCpf),
        InscricaoEstadual = r.InscricaoEstadual ?? "",
        EnderecoLinha = FormatLogradouro(r.Endereco),
        Bairro = r.Endereco.Bairro ?? "",
        MunicipioUf = FormatMunicipioUf(r.Endereco.Municipio, r.Endereco.Uf),
        Cep = Format.Cep(r.Endereco.Cep),
        Fone = Format.Fone(r.Telefone),
        Pais = r.Endereco.Pais ?? "",
    };

    private static EnderecoVm BuildEnderecoDestinatario(Destinatario d) => new()
    {
        RazaoSocial = d.RazaoSocial ?? "",
        CnpjCpf = Format.CnpjOuCpf(d.CnpjCpf),
        InscricaoEstadual = d.InscricaoEstadual ?? "",
        EnderecoLinha = FormatLogradouro(d.Endereco),
        Bairro = d.Endereco.Bairro ?? "",
        MunicipioUf = FormatMunicipioUf(d.Endereco.Municipio, d.Endereco.Uf),
        Cep = Format.Cep(d.Endereco.Cep),
        Fone = Format.Fone(d.Telefone),
        Pais = d.Endereco.Pais ?? "",
    };

    private static string FormatLogradouro(Endereco e)
    {
        var line = $"{e.Logradouro}, {e.Numero}".Trim(' ', ',');
        if (!string.IsNullOrWhiteSpace(e.Complemento)) line += $" - {e.Complemento}";
        if (!string.IsNullOrWhiteSpace(e.Bairro)) line += $" - {e.Bairro}";
        return line;
    }

    private static string FormatMunicipioUf(string? municipio, string? uf) =>
        string.IsNullOrWhiteSpace(municipio) ? "" : $"{municipio} - {uf}";

    private static string FormatMunicipioCompleto(string? municipio, string? uf, int? codigoMunicipio) =>
        string.IsNullOrWhiteSpace(municipio) ? "" : $"{municipio} - {uf} - {codigoMunicipio:000}";

    // ------------------------------------------------------------------
    // Status watermark / protocolo (rlb_09_ObsBeforePrint, retrato_layout.md §1 "Watermark")
    // ------------------------------------------------------------------

    private static void BuildStatusWatermarkAndProtocolo(DacteViewModel vm, CteDocument cte, DacteOptions options)
    {
        var ide = cte.Identificacao;
        var prot = cte.Protocolo;

        // --- Watermark / status banner ---
        if (ide.TipoAmbiente == TipoAmbiente.Homologacao)
        {
            vm.MensagemStatus = string.IsNullOrWhiteSpace(prot?.NumeroProtocolo)
                ? "CT-e NÃO ENVIADO, SEM VALOR FISCAL - HOMOLOGAÇÃO"
                : "CT-e SEM VALOR FISCAL - AMBIENTE DE HOMOLOGAÇÃO";
        }
        else if (prot is not null && (prot.CodigoStatus ?? 0) > 0)
        {
            if (prot.CodigoStatus == 101 || options.Cancelada) vm.MensagemStatus = "CT-e CANCELADO";
            else if (prot.CodigoStatus == 110) vm.MensagemStatus = "CT-e DENEGADO";
            else if (prot.CodigoStatus is not (101 or 110 or 100)) vm.MensagemStatus = prot.MotivoStatus;
        }
        else if (string.IsNullOrWhiteSpace(prot?.NumeroProtocolo))
        {
            vm.MensagemStatus = "CT-e NÃO ENVIADO, SEM VALOR FISCAL";
        }

        // --- Barcode / protocolo legend (RLCTeBeforePrint tpEmis branches, retrato_layout.md §4/§5) ---
        bool cStatAutorizado = prot?.CodigoStatus is 100 or 101 or 110;
        bool normalEmissionPath = ide.TipoEmissao is TipoEmissao.Normal or TipoEmissao.SCAN or TipoEmissao.SVCAN
            or TipoEmissao.SVCRS or TipoEmissao.SVCSP
            || ((ide.TipoEmissao is TipoEmissao.Contingencia or TipoEmissao.FSDA) && cStatAutorizado);

        vm.BarcodeDigitos = Format.OnlyDigits(cte.Id);

        if (normalEmissionPath)
        {
            vm.UsarBarcodeContingencia = false;
            vm.RotuloProtocolo = prot?.CodigoStatus switch
            {
                101 => "PROTOCOLO DE HOMOLOGAÇÃO DE CANCELAMENTO",
                110 => "PROTOCOLO DE DENEGAÇÃO DE USO",
                _ => "PROTOCOLO DE AUTORIZAÇÃO DE USO",
            };
            vm.TextoProtocolo = !string.IsNullOrWhiteSpace(options.Protocolo)
                ? options.Protocolo!
                : (prot is null ? "" : $"{prot.NumeroProtocolo}   {Format.DataHoraBr(prot.DataHoraRecebimento)}");
        }
        else
        {
            // True contingency / EPEC: ACBr draws a barcode of GerarChaveContingencia(fpCTe), an
            // externally-implemented algorithm not present in the analyzed source - see class remarks
            // and docs/limitations.md. We flag the mode but cannot fabricate the key/barcode contents.
            vm.UsarBarcodeContingencia = true;
            vm.RotuloProtocolo = "DADOS DO CT-E";
            vm.TextoProtocolo = "";
            vm.BarcodeDigitos = "";
        }
    }

    // ------------------------------------------------------------------
    // Peso / cubagem / volumes (infCarga.InfQ[], retrato_layout.md §5)
    // ------------------------------------------------------------------

    private static void BuildPesoCubagemVolumes(DacteViewModel vm, InformacoesCarga infCarga)
    {
        var pesoBruto = new List<string>();
        var pesoBaseCalculo = new List<string>();
        var pesoAferido = new List<string>();
        var cubagem = new List<string>();
        var volumes = new List<LinhaMedidaVm>();

        foreach (var q in infCarga.Quantidades)
        {
            switch (q.Unidade)
            {
                case UnidadeMedidaCarga.M3:
                    cubagem.Add(Format.Quantidade(q.Quantidade));
                    break;
                case UnidadeMedidaCarga.Kg:
                case UnidadeMedidaCarga.Ton:
                {
                    var valor = q.Unidade == UnidadeMedidaCarga.Ton ? q.Quantidade * 1000 : q.Quantidade;
                    var texto = Format.Quantidade(valor);
                    var tpMed = (q.TipoMedida ?? "").Trim().ToUpperInvariant();
                    if (tpMed == "PESO BRUTO") pesoBruto.Add(texto);
                    else if (tpMed is "PESO BASE DE CALCULO" or "PESO BC") pesoBaseCalculo.Add(texto);
                    else pesoAferido.Add(texto);
                    break;
                }
                case UnidadeMedidaCarga.Unidade:
                case UnidadeMedidaCarga.Litros:
                case UnidadeMedidaCarga.MMBTU:
                    volumes.Add(new LinhaMedidaVm
                    {
                        TipoMedida = q.TipoMedida ?? "",
                        UnidadeMedida = UnidadeMedidaTexto(q.Unidade),
                        Quantidade = Format.Quantidade(q.Quantidade),
                    });
                    break;
            }
        }

        vm.PesoBrutoKg = string.Join("\n", pesoBruto);
        vm.PesoBaseCalculoKg = string.Join("\n", pesoBaseCalculo);
        vm.PesoAferidoKg = string.Join("\n", pesoAferido);
        vm.CubagemM3 = string.Join("\n", cubagem);
        vm.LinhasMedida = volumes;
    }

    private static string UnidadeMedidaTexto(UnidadeMedidaCarga u) => u switch
    {
        UnidadeMedidaCarga.Kg => "KG",
        UnidadeMedidaCarga.Ton => "TON",
        UnidadeMedidaCarga.Litros => "LITROS",
        UnidadeMedidaCarga.MMBTU => "MMBTU",
        UnidadeMedidaCarga.Unidade => "UNIDADE",
        UnidadeMedidaCarga.M3 => "M3",
        _ => "",
    };

    // ------------------------------------------------------------------
    // Seguro (legacy <3.00 vs moderno >=3.00, retrato_layout.md §7 "Duplicate/overlapping insurance")
    // ------------------------------------------------------------------

    private static void BuildSeguro(DacteViewModel vm, InfoCTeNormal? infNormal, Identificacao ide, bool isOS)
    {
        if (infNormal is null) return;

        if (ide.Versao < 3.00 && infNormal.Seguros.Count > 0)
        {
            var s = infNormal.Seguros[0];
            vm.SeguroLegado = new SeguroVm
            {
                Responsavel = ResponsavelSeguroTexto(s.Responsavel),
                Seguradora = s.NomeSeguradora ?? "",
                Apolice = s.NumeroApolice ?? "",
                Averbacao = s.NumeroAverbacao ?? "",
            };
        }

        // A CT-e with versao>=3.00, modelo<>67 and modal<>Multimodal prints NO seguro block at all -
        // this is a real content gap in the original layout (retrato_layout.md §7), reproduced here
        // faithfully rather than "fixed".
        if (isOS || ide.Modal == Modal.Multimodal)
        {
            vm.SegurosModernos = infNormal.Seguros.Select(s => new SeguroVm
            {
                Responsavel = ResponsavelSeguroTexto(s.Responsavel),
                Seguradora = s.NomeSeguradora ?? "",
                Apolice = s.NumeroApolice ?? "",
                Averbacao = s.NumeroAverbacao ?? "",
            }).ToList();
        }
    }

    private static string ResponsavelSeguroTexto(ResponsavelSeguro? r) => r switch
    {
        ResponsavelSeguro.Remetente => "REMETENTE",
        ResponsavelSeguro.Expedidor => "EXPEDIDOR",
        ResponsavelSeguro.Recebedor => "RECEBEDOR",
        ResponsavelSeguro.Destinatario => "DESTINATÁRIO",
        ResponsavelSeguro.EmitenteCTe => "EMITENTE DO CT-E",
        ResponsavelSeguro.TomadorServico => "TOMADOR DO SERVIÇO",
        _ => "",
    };

    // ------------------------------------------------------------------
    // Valor da prestação / componentes (retrato_layout.md §5)
    // ------------------------------------------------------------------

    private static void BuildValoresEComponentes(DacteViewModel vm, CteDocument cte)
    {
        vm.ValorTotalServico = Format.Moeda(cte.ValorPrestacao.ValorTotalPrestacao);
        vm.ValorTotalReceber = Format.Moeda(cte.ValorPrestacao.ValorReceber);

        // Faithful reproduction of the original's own limitation: components beyond the 12th
        // (index 11) are silently dropped - there is no overflow/pagination for this list.
        vm.ComponentesPrestacao = cte.ValorPrestacao.Componentes
            .Take(12)
            .Select(c => (c.Nome ?? "", Format.Moeda(c.Valor)))
            .ToList();
    }

    // ------------------------------------------------------------------
    // ICMS (per-CST dispatch table, retrato_layout.md §5 "ICMS")
    // ------------------------------------------------------------------

    private static void BuildIcms(DacteViewModel vm, CteDocument cte, double versao, bool isOS)
    {
        var icms = cte.Icms;
        var vmIcms = new IcmsVm
        {
            SituacaoTributaria = CstDescricao(icms.SituacaoTributaria),
            MostrarColunaReducaoBc = true,
            MostrarColunaIcmsSt = versao < 3.00,
        };

        switch (icms.SituacaoTributaria)
        {
            case CstIcms.Cst00:
                vmIcms.BaseCalculo = Format.Moeda(icms.BaseCalculo);
                vmIcms.Aliquota = Format.Moeda(icms.AliquotaIcms);
                vmIcms.ValorIcms = Format.Moeda(icms.ValorIcms);
                break;
            case CstIcms.Cst20:
            case CstIcms.Cst90:
            case CstIcms.IcmsOutraUF:
                vmIcms.BaseCalculo = Format.Moeda(icms.BaseCalculo);
                vmIcms.Aliquota = Format.Moeda(icms.AliquotaIcms);
                vmIcms.ValorIcms = Format.Moeda(icms.ValorIcms);
                vmIcms.PercentualReducaoBc = Format.Moeda(icms.PercentualReducaoBaseCalculo);
                break;
            case CstIcms.Cst60:
                vmIcms.BaseCalculo = Format.Moeda(icms.BaseCalculo);
                vmIcms.Aliquota = Format.Moeda(icms.AliquotaIcms);
                if (versao >= 3.00) vmIcms.ValorIcms = Format.Moeda(icms.ValorIcms);
                else vmIcms.IcmsStLegado = Format.Moeda(icms.ValorIcms);
                break;
            default:
                // Cst40/Cst41/Cst45/Cst51/IcmsSN: isenta/não-tributada/suspensão/diferimento/Simples
                // Nacional - no vBC/pICMS/vICMS in the schema, all fields stay blank.
                break;
        }

        vm.Icms = vmIcms;

        if (isOS && cte.TributosFederais is not null)
        {
            vm.TributosFederais = new TributosFederaisVm
            {
                Pis = Format.Moeda(cte.TributosFederais.Pis),
                Cofins = Format.Moeda(cte.TributosFederais.Cofins),
                Ir = Format.Moeda(cte.TributosFederais.Ir),
                Inss = Format.Moeda(cte.TributosFederais.Inss),
                Csll = Format.Moeda(cte.TributosFederais.Csll),
            };
        }
    }

    private static string CstDescricao(CstIcms cst) => cst switch
    {
        CstIcms.Cst00 => "00 - TRIBUTAÇÃO NORMAL DO ICMS",
        CstIcms.Cst20 => "20 - TRIBUTAÇÃO COM BASE DE CÁLCULO REDUZIDA DO ICMS",
        CstIcms.Cst40 => "40 - ICMS ISENÇÃO",
        CstIcms.Cst41 => "41 - ICMS NÃO TRIBUTADO",
        CstIcms.Cst45 => "45 - ICMS NÃO TRIBUTADO / SUSPENSÃO",
        CstIcms.Cst51 => "51 - ICMS DIFERIMENTO",
        CstIcms.Cst60 => "60 - ICMS COBRADO POR SUBSTITUIÇÃO TRIBUTÁRIA ANTERIORMENTE",
        CstIcms.Cst90 => "90 - ICMS OUTROS",
        CstIcms.IcmsOutraUF => "90 - ICMS PARTILHA COM A UF DE TÉRMINO DA PRESTAÇÃO",
        CstIcms.IcmsSN => "90 - ICMS SIMPLES NACIONAL",
        _ => "",
    };

    // ------------------------------------------------------------------
    // Documentos originários (Itens/cdsDocumentos, retrato_layout.md §7)
    // ------------------------------------------------------------------

    private static void BuildDocumentosOriginarios(DacteViewModel vm, InfoCTeNormal? infNormal, bool isOS)
    {
        if (infNormal is null || isOS) return; // rlb_07_HeaderItens is disabled outright for modelo 67

        var doc = infNormal.DocumentosOriginarios;
        var numNota = vm.NumeroCTe; // see class remarks: inferred convention

        var itens = new List<(string Tipo, string Cnpj, string Documento)>();

        foreach (var nf in doc.NotasFiscais)
            itens.Add(("NF", numNota, $"{nf.Serie}-{nf.Numero}"));

        foreach (var nfe in doc.NotasFiscaisEletronicas)
            itens.Add(("NF-e", numNota, Format.ChaveAcesso(nfe.Chave)));

        foreach (var outro in doc.Outros)
            itens.Add((TipoDocumentoOutrosTexto(outro.Tipo), numNota, outro.Numero ?? outro.DescricaoOutros ?? ""));

        foreach (var emissor in infNormal.DocumentosAnteriores)
        {
            var cnpjEmissor = Format.CnpjOuCpf(emissor.CnpjCpf);
            foreach (var papel in emissor.DocumentosPapel)
                itens.Add((TipoDocumentoAnteriorTexto(papel.Tipo), cnpjEmissor, $"{papel.Serie}-{papel.Numero}"));
            foreach (var eletronico in emissor.DocumentosEletronicos)
                itens.Add(("CT-e", cnpjEmissor, Format.ChaveAcesso(eletronico.Chave)));
        }

        var linhas = new List<(string, string, string, string)>();
        for (int i = 0; i < itens.Count; i += 2)
        {
            (string Tipo, string Cnpj, string Documento) col1 = itens[i];
            (string Tipo, string Cnpj, string Documento) col2 = i + 1 < itens.Count ? itens[i + 1] : ("", "", "");
            linhas.Add((col1.Tipo, $"{col1.Cnpj}       {col1.Documento}", col2.Tipo, string.IsNullOrEmpty(col2.Tipo) ? "" : $"{col2.Cnpj}       {col2.Documento}"));
        }

        vm.LinhasDocumentosOriginarios = linhas;
    }

    private static string TipoDocumentoOutrosTexto(TipoDocumentoOutros t) => t switch
    {
        TipoDocumentoOutros.Declaracao => "DECLARAÇÃO",
        TipoDocumentoOutros.Dutoviario => "DUTOVIÁRIO",
        TipoDocumentoOutros.CFeSAT => "CF-e-SAT",
        TipoDocumentoOutros.NFCe => "NFC-e",
        _ => "OUTROS",
    };

    private static string TipoDocumentoAnteriorTexto(TipoDocumentoAnteriorPapel t) => t.ToString();

    // ------------------------------------------------------------------
    // Complemento / Anulação / Substituição
    // ------------------------------------------------------------------

    private static void BuildComplementoESubstituicao(DacteViewModel vm, CteDocument cte, Identificacao ide, bool isOS)
    {
        if (ide.TipoCTe == TipoCTe.Complemento && cte.CteComplementado is not null)
        {
            vm.ChavesComplementadas = cte.CteComplementado.ChavesComplementadas
                .Select(Format.ChaveAcesso)
                .ToList();
        }

        if (ide.TipoCTe is TipoCTe.Anulacao or TipoCTe.Substituto)
        {
            vm.MostrarAnuladoSubstituido = true;
            var rotuloCte = isOS ? "CT-E OS" : "CT-E";

            if (ide.TipoCTe == TipoCTe.Anulacao && cte.CteAnulado is not null)
            {
                vm.RotuloChaveAnuladoSubstituido = $"CHAVE {rotuloCte} ANULADO";
                vm.ChaveAnuladoSubstituido = Format.ChaveAcesso(cte.CteAnulado.Chave);
                vm.MostrarChaveAnulacaoSubstituicao = false;
            }
            else if (ide.TipoCTe == TipoCTe.Substituto && cte.InfoNormal?.CteSubstituto is not null)
            {
                vm.RotuloChaveAnuladoSubstituido = $"CHAVE {rotuloCte} SUBSTITUÍDO";
                vm.ChaveAnuladoSubstituido = Format.ChaveAcesso(cte.InfoNormal.CteSubstituto.ChaveCteSubstituido);
                vm.MostrarChaveAnulacaoSubstituicao = true;
                vm.ChaveAnulacaoSubstituicao = Format.ChaveAcesso(cte.InfoNormal.CteSubstituto.ChaveCteAnulacao);
            }
        }
    }

    // ------------------------------------------------------------------
    // Modal-specific blocks
    // ------------------------------------------------------------------

    private static void BuildModais(DacteViewModel vm, InfoCTeNormal? infNormal, Identificacao ide, double versao, bool isOS)
    {
        if (infNormal is null) return;

        if (ide.Modal == Modal.Rodoviario)
        {
            vm.ModalRodoviario = isOS
                ? BuildModalRodoviarioOS(infNormal.RodoviarioOS)
                : BuildModalRodoviario(infNormal.Rodoviario, infNormal);
        }
        else if (ide.Modal == Modal.Aereo)
        {
            vm.ModalAereo = BuildModalAereo(infNormal.Aereo, ide);
        }
        else if (ide.Modal == Modal.Aquaviario)
        {
            vm.ModalAquaviario = BuildModalAquaviario(infNormal.Aquaviario);
        }
        // Ferroviário/Dutoviário: this DACTE layout has no modal-specific section for either
        // (retrato_layout.md §7 - both bands are always forced to Height=0, even in ACBr's own
        // matching case branch), so nothing is built for them here.
    }

    private static ModalRodoviarioVm BuildModalRodoviario(ModalRodoviario? rodo, InfoCTeNormal infNormal)
    {
        rodo ??= new ModalRodoviario();
        var vm = new ModalRodoviarioVm
        {
            ModeloOS = false,
            RntrcOuTaf = rodo.Rntrc ?? "",
            CiotOuRegistroEstadual = rodo.Ciot ?? "",
            Lotacao = rodo.Lotacao == Models.Lotacao.Sim,
            LotacaoTexto = rodo.Lotacao == Models.Lotacao.Sim ? "SIM" : "NÃO",
            DataPrevistaEntrega = Format.DataBr(rodo.DataPrevistaEntrega),
        };

        vm.Veiculos = rodo.Veiculos.Select(v => (
            v.TipoVeiculo == Models.TipoVeiculo.Reboque ? "Reboque" : "Tração",
            v.Placa ?? "",
            v.Uf ?? "",
            v.Proprietario?.Rntrc ?? "")).ToList();

        vm.Motoristas = rodo.Motoristas.Select(m => $"{m.Nome} - {Format.Cpf(m.Cpf)}").ToList();
        vm.Lacres = rodo.Lacres.Select(l => l.Numero ?? "").ToList();
        vm.ValesPedagio = rodo.ValesPedagio.Select(v => (
            Format.Cnpj(v.CnpjFornecedora), v.NumeroCompra ?? "", Format.Cnpj(v.CnpjPagador), Format.Moeda(v.Valor))).ToList();

        return vm;
    }

    private static ModalRodoviarioVm BuildModalRodoviarioOS(ModalRodoviarioOS? rodoOS)
    {
        rodoOS ??= new ModalRodoviarioOS();
        // Field reuse mirrors ACBr's own relabeling for modelo 67 (modalRodoviarioMod67,
        // retrato_layout.md rlb_10_ModRodFracionado notes): RNTRC->TAF, CIOT->Nº registro estadual,
        // Lotação->Placa do veículo, Data prevista->RENAVAM.
        return new ModalRodoviarioVm
        {
            ModeloOS = true,
            RntrcOuTaf = rodoOS.Taf ?? "",
            CiotOuRegistroEstadual = rodoOS.NumeroRegistroEstadual ?? "",
            LotacaoTexto = rodoOS.Veiculo?.Placa ?? "",
            DataPrevistaEntrega = rodoOS.Veiculo?.Renavam ?? "",
            Veiculos = rodoOS.Veiculo is null
                ? new()
                : new() { ("", rodoOS.Veiculo.Placa ?? "", rodoOS.Veiculo.Uf ?? "", "") },
        };
    }

    private static ModalAereoVm BuildModalAereo(ModalAereo? aereo, Identificacao ide)
    {
        aereo ??= new ModalAereo();
        return new ModalAereoVm
        {
            Tarifa = aereo.Tarifa is null ? "" : $"CL {aereo.Tarifa.Classe}  COD {aereo.Tarifa.Codigo}  VALOR {Format.Moeda(aereo.Tarifa.Valor)}",
            RetiradaCarga = ide.Retira == Retira.Sim ? "SIM" : "NÃO",
            DetalheRetirada = ide.DetalheRetira ?? "",
            NumeroOca = aereo.NumeroOCA ?? "",
            NumeroMinuta = aereo.NumeroMinuta?.ToString("0000000000") ?? "",
            ContaCorrente = aereo.IdentificacaoTerminal ?? "", // aereo.IdT - marked uncertain in source, see class remarks
            Dimensoes = aereo.Dimensoes ?? "",
            InstrucoesManuseio = string.Join(", ", aereo.InstrucoesManuseio),
            InformacoesComplementares = aereo.InformacoesComplementaresManuseio ?? "",
        };
    }

    private static ModalAquaviarioVm BuildModalAquaviario(ModalAquaviario? aquav)
    {
        aquav ??= new ModalAquaviario();
        return new ModalAquaviarioVm
        {
            PortoEmbarque = aquav.PortoEmbarque ?? "",
            PortoTransbordo = aquav.PortoTransbordo ?? "",
            PortoDestino = aquav.PortoDestino ?? "",
            Navio = aquav.NomeNavio ?? "",
            Viagem = aquav.NumeroViagem ?? "",
            Balsas = string.Join("/", aquav.Balsas),
            Direcao = DirecaoTexto(aquav.Direcao),
            TipoNavegacao = aquav.TipoNavegacao == TipoNavegacao.Cabotagem ? "CABOTAGEM" : "INTERIOR",
            ValorAfrmm = Format.Moeda(aquav.ValorAfrmm),
            // aquav.vPrest is, confusingly, the AFRMM calculation base (rllBCAFRMM), not the freight value.
            BaseCalculoAfrmm = Format.Moeda(aquav.ValorPrestacao),
        };
    }

    private static string DirecaoTexto(DirecaoAquaviaria? d) => d switch
    {
        DirecaoAquaviaria.Norte => "NORTE",
        DirecaoAquaviaria.Leste => "LESTE",
        DirecaoAquaviaria.Sul => "SUL",
        DirecaoAquaviaria.Oeste => "OESTE",
        _ => "",
    };

    // ------------------------------------------------------------------
    // Fluxo de carga aéreo (rlb_Fluxo_Carga, only versao>=3.00, modal=Aereo, modelo<>67)
    // ------------------------------------------------------------------

    private static void BuildFluxoCarga(DacteViewModel vm, Complemento? compl)
    {
        var fluxo = compl?.Fluxo;
        if (fluxo is null) return;
        vm.FluxoOrigem = fluxo.Origem;
        vm.FluxoDestino = fluxo.Destino;
        vm.FluxoRota = fluxo.Rota;
    }

    // ------------------------------------------------------------------
    // CT-e OS: informações da prestação do serviço
    // ------------------------------------------------------------------

    private static void BuildInformacoesServicoOS(DacteViewModel vm, InfoCTeNormal? infNormal, bool isOS)
    {
        if (!isOS || infNormal?.InformacoesServico is null) return;
        var svc = infNormal.InformacoesServico;
        vm.InfoServicoQuantidade = svc.QuantidadeCarga is null ? "" : Format.Quantidade(svc.QuantidadeCarga);
        vm.InfoServicoDescricao = svc.DescricaoServico ?? "";
    }

    // ------------------------------------------------------------------
    // Observações / uso exclusivo do emissor / reservado ao fisco (rlb_16_DadosExcEmitente)
    // ------------------------------------------------------------------

    private static void BuildObservacoesEFisco(DacteViewModel vm, CteDocument cte)
    {
        var compl = cte.Complemento;

        if (!string.IsNullOrWhiteSpace(compl?.Observacoes))
        {
            vm.LinhasObservacoes = compl!.Observacoes!
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }

        vm.ObservacoesContribuinte = (compl?.ObservacoesContribuinte ?? new List<ObservacaoItem>())
            .Select(o => $"{o.Campo}: {o.Texto}")
            .ToList();

        var fiscoLinhas = new List<string>();
        if (!string.IsNullOrWhiteSpace(cte.Icms.InformacoesAdicionaisFisco))
            fiscoLinhas.Add(cte.Icms.InformacoesAdicionaisFisco!);
        fiscoLinhas.AddRange((compl?.ObservacoesFisco ?? new List<ObservacaoItem>()).Select(o => $"{o.Campo}: {o.Texto}"));
        vm.ObservacoesFisco = fiscoLinhas;
        vm.InformacoesAdicionaisFisco = cte.Icms.InformacoesAdicionaisFisco;

        // fluxoCargaVersao30: only versao>=3.00, modelo<>67, modal=Aereo.
        var ide = cte.Identificacao;
        if (ide.Versao >= 3.00 && ide.Modelo != ModeloDocumento.CTeOS && ide.Modal == Modal.Aereo)
            BuildFluxoCarga(vm, compl);
    }

    // ------------------------------------------------------------------
    // Modal display text (TpModalToStrText)
    // ------------------------------------------------------------------

    private static string ModalTexto(Modal? m) => m switch
    {
        Modal.Rodoviario => "RODOVIÁRIO",
        Modal.Aereo => "AÉREO",
        Modal.Aquaviario => "AQUAVIÁRIO",
        Modal.Ferroviario => "FERROVIÁRIO",
        Modal.Dutoviario => "DUTOVIÁRIO",
        Modal.Multimodal => "MULTIMODAL",
        _ => "",
    };

    // ------------------------------------------------------------------
    // Canhoto summary (GetTextoResumoCanhoto, ACBrCTeDACTeRL.pas - quoted verbatim in structure)
    // ------------------------------------------------------------------

    private static string BuildTextoResumoCanhoto(CteDocument cte)
    {
        var ide = cte.Identificacao;
        var result = $"EMIT: {cte.Emitente.RazaoSocial} - EMISSÃO: {Format.DataBr(ide.DataHoraEmissao)}  -  TOMADOR: ";

        if (string.IsNullOrWhiteSpace(ide.Toma4?.RazaoSocial))
        {
            result += ide.Toma03?.Tomador switch
            {
                Tomador.Remetente => cte.Remetente?.RazaoSocial ?? "",
                Tomador.Expedidor => cte.Expedidor?.RazaoSocial ?? "",
                Tomador.Recebedor => cte.Recebedor?.RazaoSocial ?? "",
                Tomador.Destinatario => cte.Destinatario?.RazaoSocial ?? "",
                _ => "",
            };
        }
        else
        {
            result += ide.Toma4!.RazaoSocial;
        }

        result += $" - VALOR A RECEBER: R$ {Format.Moeda(cte.ValorPrestacao.ValorReceber)}";
        return result;
    }
}
