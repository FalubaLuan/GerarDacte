using DacteNet.Rendering.Primitives;
using DacteNet.ViewModel;

namespace DacteNet.Rendering.A5;

/// <summary>
/// Renders a <see cref="DacteViewModel"/> onto the "simplified" A5 DACTE layout, reproducing
/// ACBrCTeDACTeRLRetratoA5.pas (analysis/retrato_a5_layout.md) - a landscape A5 sheet
/// (210mm x 148mm; standard A5 portrait rotated sideways), band-by-band in the same design-unit
/// coordinate system as <see cref="Rendering.A4.DacteA4Renderer"/> (1 raw unit = 0.75pt).
///
/// Scope note (see docs/limitations.md): this renderer covers every band that carries CT-e data
/// (canhoto, cabeçalho, dados do DACTE, dados da nota fiscal/ICMS, valor da prestação, documentos
/// originários with its own pagination, observações + RNTRC/CIOT/lotação + status watermark,
/// rodapé). The road-modal "lotação" vale-pedágio table (rlb_11_ModRodLot104) and the
/// aéreo/aquaviário specialty blocks that exist in the A4 layout are not present in ACBr's own A5
/// report in the analyzed source's most detail-rich form and were not fully re-derived here given the
/// scope of this pass - they are intentionally omitted from the A5 output (A4 is the recommended
/// paper size for CT-e's using those modals).
/// </summary>
public sealed class DacteA5Renderer
{
    private const double PageWidthMm = 210.0;
    private const double PageHeightMm = 148.0;
    private const double MarginMm = 6.88;

    private static DacteFont F(double sizePt, bool bold = false) => DacteFont.TimesNewRoman(sizePt, bold);

    public PdfDocument Render(DacteViewModel vm)
    {
        var doc = new PdfDocument();
        double marginPt = Layout.PtFromMm(MarginMm);
        var canvas = new ReportCanvas(doc, Layout.PtFromMm(PageWidthMm), Layout.PtFromMm(PageHeightMm),
            marginPt, marginPt, marginPt, marginPt);

        RenderRecibo(canvas, vm);
        RenderCabecalho(canvas, vm);
        RenderDadosDacte(canvas, vm);
        RenderDadosNotaFiscal(canvas, vm);
        RenderValorPrestacao(canvas, vm);
        RenderDocumentosOriginarios(canvas, vm);
        RenderObservacoes(canvas, vm);
        RenderRodapeSistema(canvas, vm);

        return doc;
    }

    private static void EnsureSpace(ReportCanvas canvas, double heightRl)
    {
        if (canvas.RemainingHeightPt < Layout.Pt(heightRl)) canvas.NewPage();
    }

    // rlb_01_Recibo (§3.1) / rlb_01_Recibo_Aereo (§3.2)
    private static void RenderRecibo(ReportCanvas canvas, DacteViewModel vm)
    {
        if (vm.MostrarAnuladoSubstituido) return;
        const double h = 56;
        EnsureSpace(canvas, h);
        canvas.Rect(0, 0, 741, 57);
        canvas.Text(6, 1, 732, 13,
            "DECLARO QUE RECEBI OS VOLUMES DESTE CONHECIMENTO EM PERFEITO ESTADO PELO QUE DOU POR CUMPRIDO O PRESENTE CONTRATO DE TRANSPORTE",
            F(6), TextAlign.Center);
        canvas.Line(1, 14, 740, 14, 0.5);
        canvas.Text(6, 18, 30, 12, "NOME", F(6.75));
        canvas.Text(6, 37, 15, 12, "RG", F(6.75));
        canvas.Text(207, 42, 262, 13, "ASSINATURA / CARIMBO", F(6), TextAlign.Center);
        canvas.Text(480, 15, 108, 9, "CHEGADA DATA/HORA", F(5.25));
        canvas.Text(480, 20, 108, 13, "__/__/__  __:__", F(9), TextAlign.Center);
        canvas.Text(480, 35, 108, 9, "SAÍDA DATA/HORA", F(5.25));
        canvas.Text(480, 40, 108, 13, "__/__/__  __:__", F(9), TextAlign.Center);
        canvas.Text(647, 15, 28, 13, vm.RotuloCTe, F(8.25, true));
        canvas.Text(605, 27, 14, 12, "N.", F(6.75));
        canvas.Text(638, 27, 86, 16, vm.NumeroCTe, F(9.75, true), TextAlign.Right);
        canvas.Text(605, 41, 30, 12, "SÉRIE:", F(6.75));
        canvas.Text(655, 41, 50, 13, vm.Serie, F(8.25, true), TextAlign.Center);
        canvas.AdvanceBand(h);
    }

    // rlb_02_Cabecalho (§3.3) - static-label coordinates confirmed directly against
    // ACBrCTeDACTeRLRetratoA5.dfm (rlLabel6/8/21/23/25/33/74/2/9/28/77).
    private static void RenderCabecalho(ReportCanvas canvas, DacteViewModel vm)
    {
        const double h = 148;
        EnsureSpace(canvas, h);
        canvas.Rect(0, 0, 741, 149);

        canvas.Text(7, 1, 322, 24, vm.Emitente.RazaoSocial, F(11, true), TextAlign.Center);
        canvas.Memo(113, 26, 216, vm.Emitente.LinhasEnderecoCompleto, F(9), lineHeightRl: 10);

        canvas.Text(371, 1, 218, 17, vm.TituloDacte, F(12, true), TextAlign.Center);
        canvas.Text(344, 16, 278, 14, vm.SubtituloDacte, F(8, true), TextAlign.Center);

        canvas.Text(640, 1, 76, 16, "MODAL", F(8.25, true), TextAlign.Center);
        canvas.Text(633, 16, 96, 15, vm.Modal, F(8.25, true), TextAlign.Center);
        canvas.Text(333, 30, 32, 8, "MODELO", F(5.25), TextAlign.Center);
        canvas.Text(334, 38, 30, 15, vm.ModeloOS ? "67" : "57", F(8.25, true), TextAlign.Center);
        canvas.Text(367, 30, 22, 8, "SÉRIE", F(5.25), TextAlign.Center);
        canvas.Text(368, 38, 20, 15, vm.Serie, F(8.25, true), TextAlign.Center);
        canvas.Text(392, 30, 70, 9, "NÚMERO", F(5.25), TextAlign.Center);
        canvas.Text(392, 38, 70, 15, vm.NumeroCTe, F(8.25, true), TextAlign.Center);
        canvas.Text(466, 30, 42, 9, "FOLHA", F(5.25), TextAlign.Center); // page/total-pages token not tracked - see docs/limitations.md
        canvas.Text(510, 30, 95, 9, "DATA E HORA DE EMISSÃO", F(5.25));
        canvas.Text(510, 38, 58, 13, vm.DataHoraEmissao, F(8.25, true));
        canvas.Text(616, 30, 120, 8, "INSC. SUFRAMA DO DESTINATÁRIO", F(5.25));
        canvas.Text(616, 38, 56, 12, vm.Destinatario?.CnpjCpf ?? "", F(6.75));

        canvas.Barcode128(337, 58, 398, 28, vm.BarcodeDigitos);
        canvas.Text(334, 90, 58, 11, "Chave de acesso", F(6, true));
        canvas.Text(336, 100, 402, 14, vm.ChaveAcessoFormatada, F(8.25, true), TextAlign.Center);

        canvas.Text(4, 116, 46, 8, "TIPO DO CT-E", F(5.25));
        canvas.Text(4, 124, 76, 15, vm.TipoCteTexto, F(6.75));
        canvas.Text(107, 116, 61, 8, "TIPO DO SERVIÇO", F(5.25));
        canvas.Text(107, 124, 77, 15, vm.TipoServicoTexto, F(6.75));
        canvas.Text(212, 117, 81, 8, "TOMADOR DO SERVIÇO", F(5.25));
        canvas.Text(212, 126, 81, 15, vm.TomadorDescricaoTexto, F(6.75));
        canvas.Text(334, 117, 56, 8, vm.RotuloProtocolo, F(6, true));
        canvas.Text(336, 126, 402, 15, vm.TextoProtocolo, F(6, true), TextAlign.Center);

        canvas.AdvanceBand(h);
    }

    // rlb_03_DadosDACTe (§3.4) + rlb_03_DadosRedespachoExpedidor (§3.5) - static-label coordinates
    // confirmed directly against ACBrCTeDACTeRLRetratoA5.dfm.
    private static void RenderDadosDacte(ReportCanvas canvas, DacteViewModel vm)
    {
        const double h = 81;
        EnsureSpace(canvas, h);
        canvas.Rect(0, 0, 741, h);

        canvas.Text(4, 1, 115, 8, "CFOP - NATUREZA DA OPERAÇÃO", F(5.25));
        canvas.Text(4, 9, 325, 15, $"{vm.Cfop} - {vm.NaturezaOperacao}", F(6.75));
        canvas.Text(336, 1, 84, 8, "ORIGEM DA PRESTAÇÃO", F(5.25));
        canvas.Text(336, 9, 195, 15, vm.MunicipioInicio, F(6.75));
        canvas.Text(542, 1, 86, 8, "DESTINO DA PRESTAÇÃO", F(5.25));
        canvas.Text(542, 9, 195, 15, vm.MunicipioFim, F(6.75));

        canvas.Text(4, 25, 42, 8, "REMETENTE", F(5.25));
        canvas.Text(48, 25, 318, 13, vm.Remetente?.RazaoSocial ?? "", F(6.75));
        canvas.Text(4, 33, 39, 8, "ENDEREÇO", F(5.25));
        canvas.Text(48, 33, 318, 13, vm.Remetente?.EnderecoLinha ?? "", F(6.75));
        canvas.Text(4, 49, 38, 8, "MUNICÍPIO", F(5.25));
        canvas.Text(48, 49, 234, 19, vm.Remetente?.MunicipioUf ?? "", F(6.75));
        canvas.Text(284, 49, 15, 8, "CEP", F(5.25));
        canvas.Text(301, 49, 64, 13, vm.Remetente?.Cep ?? "", F(6.75));
        canvas.Text(4, 58, 34, 8, "CNPJ/CPF", F(5.25));
        canvas.Text(48, 58, 124, 13, vm.Remetente?.CnpjCpf ?? "", F(6.75));
        canvas.Text(174, 58, 78, 8, "INSCRIÇÃO ESTADUAL", F(5.25));
        canvas.Text(256, 58, 109, 13, vm.Remetente?.InscricaoEstadual ?? "", F(6.75));
        canvas.Text(4, 67, 17, 8, "PAÍS", F(5.25));
        canvas.Text(48, 67, 209, 13, vm.Remetente?.Pais ?? "", F(6.75));
        canvas.Text(262, 68, 20, 8, "FONE", F(5.25));
        canvas.Text(288, 68, 77, 13, vm.Remetente?.Fone ?? "", F(6.75));

        canvas.Text(374, 25, 52, 8, "DESTINATÁRIO", F(5.25));
        canvas.Text(432, 25, 303, 13, vm.Destinatario?.RazaoSocial ?? "", F(6.75));
        canvas.Text(374, 33, 39, 8, "ENDEREÇO", F(5.25));
        canvas.Text(432, 33, 303, 13, vm.Destinatario?.EnderecoLinha ?? "", F(6.75));
        canvas.Text(374, 49, 38, 8, "MUNICÍPIO", F(5.25));
        canvas.Text(432, 49, 225, 13, vm.Destinatario?.MunicipioUf ?? "", F(6.75));
        canvas.Text(660, 49, 15, 8, "CEP", F(5.25));
        canvas.Text(677, 49, 57, 13, vm.Destinatario?.Cep ?? "", F(6.75));
        canvas.Text(374, 58, 34, 8, "CNPJ/CPF", F(5.25));
        canvas.Text(432, 58, 115, 18, vm.Destinatario?.CnpjCpf ?? "", F(6.75));
        canvas.Text(551, 58, 78, 8, "INSCRIÇÃO ESTADUAL", F(5.25));
        canvas.Text(632, 58, 102, 13, vm.Destinatario?.InscricaoEstadual ?? "", F(6.75));
        canvas.Text(374, 67, 17, 8, "PAÍS", F(5.25));
        canvas.Text(432, 67, 203, 13, vm.Destinatario?.Pais ?? "", F(6.75));
        canvas.Text(640, 67, 20, 8, "FONE", F(5.25));
        canvas.Text(664, 68, 70, 13, vm.Destinatario?.Fone ?? "", F(6.75));

        canvas.AdvanceBand(h);

        if (vm.Expedidor is null && vm.Recebedor is null) return;
        const double h2 = 65;
        EnsureSpace(canvas, h2);
        canvas.Rect(0, 0, 741, h2);

        canvas.Text(4, 1, 41, 8, "EXPEDIDOR", F(5.25));
        canvas.Text(48, 1, 318, 13, vm.Expedidor?.RazaoSocial ?? "", F(6.75));
        canvas.Text(4, 9, 39, 8, "ENDEREÇO", F(5.25));
        canvas.Text(48, 9, 318, 13, vm.Expedidor?.EnderecoLinha ?? "", F(6.75));
        canvas.Text(4, 32, 38, 8, "MUNICÍPIO", F(5.25));
        canvas.Text(48, 29, 234, 19, vm.Expedidor?.MunicipioUf ?? "", F(6.75));
        canvas.Text(284, 29, 15, 8, "CEP", F(5.25));
        canvas.Text(301, 27, 64, 13, vm.Expedidor?.Cep ?? "", F(6.75));
        canvas.Text(4, 40, 34, 8, "CNPJ/CPF", F(5.25));
        canvas.Text(48, 39, 124, 13, vm.Expedidor?.CnpjCpf ?? "", F(6.75));
        canvas.Text(174, 40, 78, 8, "INSCRIÇÃO ESTADUAL", F(5.25));
        canvas.Text(256, 37, 109, 13, vm.Expedidor?.InscricaoEstadual ?? "", F(6.75));
        canvas.Text(5, 51, 17, 8, "PAIS", F(5.25));
        canvas.Text(48, 50, 209, 13, vm.Expedidor?.Pais ?? "", F(6.75));
        canvas.Text(262, 51, 20, 8, "FONE", F(5.25));
        canvas.Text(286, 50, 77, 13, vm.Expedidor?.Fone ?? "", F(6.75));

        canvas.Text(373, 2, 44, 8, "RECEBEDOR", F(5.25));
        canvas.Text(432, 2, 307, 13, vm.Recebedor?.RazaoSocial ?? "", F(6.75));
        canvas.Text(373, 10, 39, 8, "ENDEREÇO", F(5.25));
        canvas.Text(432, 10, 307, 13, vm.Recebedor?.EnderecoLinha ?? "", F(6.75));
        canvas.Text(373, 33, 38, 8, "MUNICÍPIO", F(5.25));
        canvas.Text(432, 30, 234, 19, vm.Recebedor?.MunicipioUf ?? "", F(6.75));
        canvas.Text(652, 30, 15, 8, "CEP", F(5.25));
        canvas.Text(669, 28, 64, 13, vm.Recebedor?.Cep ?? "", F(6.75));
        canvas.Text(373, 41, 34, 8, "CNPJ/CPF", F(5.25));
        canvas.Text(432, 40, 124, 13, vm.Recebedor?.CnpjCpf ?? "", F(6.75));
        canvas.Text(558, 41, 78, 8, "INSCRIÇÃO ESTADUAL", F(5.25));
        canvas.Text(640, 38, 97, 13, vm.Recebedor?.InscricaoEstadual ?? "", F(6.75));
        canvas.Text(374, 50, 17, 8, "PAIS", F(5.25));
        canvas.Text(432, 48, 209, 13, vm.Recebedor?.Pais ?? "", F(6.75));
        canvas.Text(646, 53, 20, 8, "FONE", F(5.25));
        canvas.Text(670, 50, 68, 13, vm.Recebedor?.Fone ?? "", F(6.75));

        canvas.AdvanceBand(h2);
    }

    // rlb_04_DadosNotaFiscal (§3.6)
    private static void RenderDadosNotaFiscal(ReportCanvas canvas, DacteViewModel vm)
    {
        const double h = 65;
        EnsureSpace(canvas, h);
        canvas.Rect(0, 0, 741, h);

        canvas.Text(4, 1, 91, 8, "PRODUTO PREDOMINANTE", F(5.25));
        canvas.Text(4, 10, 141, 13, vm.ProdutoPredominante, F(6.75));
        canvas.Text(152, 1, 135, 8, "OUTRAS CARACTERÍSTICAS DA CARGA", F(5.25));
        canvas.Text(152, 10, 139, 13, vm.OutrasCaracteristicasCarga, F(6.75));
        canvas.Text(298, 1, 111, 8, "VALOR TOTAL DA MERCADORIA", F(5.25));
        canvas.Text(298, 10, 110, 13, vm.ValorTotalCarga, F(6.75, true), TextAlign.Right);

        canvas.Text(5, 24, 76, 9, "PESO BRUTO (Kg)", F(5.25), TextAlign.Center);
        canvas.Memo(5, 32, 76, Lines(vm.PesoBrutoKg), F(6.75), TextAlign.Right);
        canvas.Text(86, 24, 76, 9, "PESO BASE CÁLC. (Kg)", F(5.25), TextAlign.Center);
        canvas.Memo(86, 32, 76, Lines(vm.PesoBaseCalculoKg), F(6.75), TextAlign.Right);
        canvas.Text(166, 24, 76, 9, "PESO AFERIDO (Kg)", F(5.25), TextAlign.Center);
        canvas.Memo(166, 32, 76, Lines(vm.PesoAferidoKg), F(6.75), TextAlign.Right);
        canvas.Text(246, 24, 76, 9, "CUBAGEM (M3)", F(5.25), TextAlign.Center);
        canvas.Memo(246, 32, 76, Lines(vm.CubagemM3), F(6.75), TextAlign.Right);
        canvas.Text(328, 24, 84, 9, "QTDE. VOLUMES (Unid)", F(5.25), TextAlign.Center);
        // rlmQtdUnidMedida5 is a TRLMemo, not a single-line label: two lines per measure (tpMed, then
        // "qty unit") - the box is too narrow (84 raw units) for the A4 renderer's single-line format.
        canvas.Memo(328, 32, 84, vm.LinhasMedida.SelectMany(l => new[] { l.TipoMedida, $"{l.Quantidade}{l.UnidadeMedida}" }), F(6), TextAlign.Right, lineHeightRl: 6);

        if (vm.SeguroLegado is not null)
        {
            canvas.Text(418, 1, 84, 8, "NOME DA SEGURADORA", F(5.25));
            canvas.Text(418, 10, 319, 13, vm.SeguroLegado.Seguradora, F(6.75));
            canvas.Text(418, 24, 51, 8, "RESPONSÁVEL", F(5.25));
            canvas.Text(418, 32, 87, 13, vm.SeguroLegado.Responsavel, F(6.75));
            canvas.Text(510, 24, 75, 8, "NÚMERO DA APÓLICE", F(5.25));
            canvas.Text(510, 32, 122, 13, vm.SeguroLegado.Apolice, F(6.75));
            canvas.Text(634, 24, 90, 8, "NÚMERO DA AVERBAÇÃO", F(5.25));
            canvas.Text(634, 32, 102, 13, vm.SeguroLegado.Averbacao, F(6.75));
        }

        canvas.Text(3, 45, 81, 8, "SITUAÇÃO TRIBUTÁRIA", F(5.25));
        canvas.Text(3, 52, 340, 13, vm.Icms.SituacaoTributaria, F(6.75));
        canvas.Text(350, 45, 66, 8, "BASE DE CÁLCULO", F(5.25));
        canvas.Text(350, 52, 95, 13, vm.Icms.BaseCalculo, F(6.75, true), TextAlign.Right);
        canvas.Text(454, 45, 39, 8, "ALÍQ. ICMS", F(5.25));
        canvas.Text(454, 52, 41, 13, vm.Icms.Aliquota, F(6.75, true), TextAlign.Right);
        canvas.Text(504, 45, 45, 8, "VALOR ICMS", F(5.25));
        canvas.Text(504, 52, 79, 13, vm.Icms.ValorIcms, F(6.75, true), TextAlign.Right);
        if (vm.Icms.MostrarColunaReducaoBc)
        {
            canvas.Text(590, 45, 59, 8, "% RED.BC.CALC.", F(5.25));
            canvas.Text(590, 52, 57, 13, vm.Icms.PercentualReducaoBc, F(6.75, true), TextAlign.Right);
        }
        if (vm.Icms.MostrarColunaIcmsSt)
        {
            canvas.Text(656, 45, 29, 8, "ICMS ST", F(5.25));
            canvas.Text(656, 52, 81, 13, vm.Icms.IcmsStLegado, F(6.75, true), TextAlign.Right);
        }

        canvas.AdvanceBand(h);
    }

    // rlb_06_ValorPrestacao (§3.8)
    private static void RenderValorPrestacao(ReportCanvas canvas, DacteViewModel vm)
    {
        const double h = 47;
        EnsureSpace(canvas, h);
        canvas.Rect(0, 0, 741, h);
        var col1 = vm.ComponentesPrestacao.Where((_, i) => i % 3 == 0).ToList();
        var col2 = vm.ComponentesPrestacao.Where((_, i) => i % 3 == 1).ToList();
        var col3 = vm.ComponentesPrestacao.Where((_, i) => i % 3 == 2).ToList();
        canvas.Text(5, 1, 22, 8, "NOME", F(5.25));
        canvas.Text(156, 1, 26, 8, "VALOR", F(5.25));
        canvas.Text(190, 1, 22, 8, "NOME", F(5.25));
        canvas.Text(342, 1, 26, 8, "VALOR", F(5.25));
        canvas.Text(377, 1, 22, 8, "NOME", F(5.25));
        canvas.Text(528, 1, 26, 8, "VALOR", F(5.25));
        canvas.Memo(5, 10, 96, col1.Select(c => c.Item1), F(6.75));
        canvas.Memo(104, 10, 78, col1.Select(c => c.Item2), F(6.75), TextAlign.Right);
        canvas.Memo(190, 10, 96, col2.Select(c => c.Item1), F(6.75));
        canvas.Memo(290, 10, 78, col2.Select(c => c.Item2), F(6.75), TextAlign.Right);
        canvas.Memo(377, 10, 96, col3.Select(c => c.Item1), F(6.75));
        canvas.Memo(476, 10, 78, col3.Select(c => c.Item2), F(6.75), TextAlign.Right);
        canvas.Text(560, 1, 96, 9, "VALOR TOTAL DO SERVIÇO", F(5.25));
        canvas.Text(658, 6, 78, 14, vm.ValorTotalServico, F(6.75, true), TextAlign.Right);
        canvas.Text(560, 23, 96, 9, "VALOR A RECEBER", F(5.25));
        canvas.Text(658, 28, 78, 14, vm.ValorTotalReceber, F(6.75, true), TextAlign.Right);
        canvas.AdvanceBand(h);
    }

    // rlb_07_HeaderItens / rlb_08_Itens (§3.9) - new page every 4 rows
    private static void RenderDocumentosOriginarios(ReportCanvas canvas, DacteViewModel vm)
    {
        var linhas = vm.LinhasDocumentosOriginarios;
        if (linhas.Count == 0) return;
        const int rowsPerPage = 4;
        int index = 0;
        while (index < linhas.Count)
        {
            var pageRows = linhas.Skip(index).Take(rowsPerPage).ToList();
            double h = 11 + pageRows.Count * 16;
            EnsureSpace(canvas, h);
            canvas.Text(5, 1, 74, 13, "TP DOC.", F(9));
            canvas.Text(81, 1, 128, 12, "CNPJ/CPF EMITENTE", F(9));
            canvas.Text(206, 1, 162, 13, "SÉRIE/NRO. DOCUMENTO", F(9));
            canvas.Text(373, 1, 74, 13, "TP DOC.", F(9));
            canvas.Text(449, 1, 128, 12, "CNPJ/CPF EMITENTE", F(9));
            canvas.Text(582, 1, 156, 13, "SÉRIE/NRO. DOCUMENTO", F(9));

            double y = 15;
            foreach (var row in pageRows)
            {
                canvas.Text(5, y, 74, 13, row.Item1, F(9));
                canvas.Text(81, y, 290, 12, row.Item2, F(9));
                canvas.Text(373, y, 74, 13, row.Item3, F(9));
                if (!string.IsNullOrEmpty(row.Item3)) canvas.Text(449, y, 290, 12, row.Item4, F(9));
                canvas.Line(1, y + 15, 740, y + 15, 0.25);
                y += 16;
            }

            canvas.AdvanceBand(h);
            index += rowsPerPage;
            if (index < linhas.Count) canvas.NewPage();
        }
    }

    // rlb_09_Obs (§3.10)
    private static void RenderObservacoes(ReportCanvas canvas, DacteViewModel vm)
    {
        const double h = 56;
        EnsureSpace(canvas, h);
        canvas.Rect(0, 0, 741, h);
        canvas.Text(304, 1, 234, 13, "Observações - Informações Complementares", F(6.75));
        canvas.Memo(304, 15, 241, vm.LinhasObservacoes, F(6));
        canvas.Text(552, 1, 186, 13, "RESERVADO AO FISCO", F(6.75));
        canvas.Memo(552, 15, 185, vm.ObservacoesFisco, F(6));

        if (vm.ModalRodoviario is not null)
        {
            canvas.Text(6, 1, 72, 8, "RNTRC DA EMPRESA", F(5.25));
            canvas.Text(84, 1, 18, 8, "CIOT", F(5.25));
            canvas.Text(84, 8, 32, 12, vm.ModalRodoviario.CiotOuRegistroEstadual, F(6.75));
            canvas.Text(154, 1, 35, 8, "LOTAÇÃO", F(5.25));
            canvas.Text(154, 8, 34, 13, vm.ModalRodoviario.LotacaoTexto, F(6.75), TextAlign.Center);
            canvas.Text(196, 1, 101, 8, "DATA PREVISTA DE ENTREGA", F(5.25));
            canvas.Text(196, 8, 69, 12, vm.ModalRodoviario.DataPrevistaEntrega, F(6.75));
            canvas.Text(6, 22, 292, 27, "ESSE CT-e DE TRANSP. ATENDE LEGISLAÇÃO DE TRANSP. RODO.EM VIGOR", F(6));
        }

        if (!string.IsNullOrWhiteSpace(vm.MensagemStatus))
            canvas.Text(7, 14, 718, 31, vm.MensagemStatus, F(20.25, true), TextAlign.Center, PdfColor.Gray);

        canvas.AdvanceBand(h);
    }

    // rlb_17_Sistema (§2 row 17)
    private static void RenderRodapeSistema(ReportCanvas canvas, DacteViewModel vm)
    {
        const double h = 13;
        EnsureSpace(canvas, h);
        if (vm.RodapeDataHoraImpressao is not null)
            canvas.Text(2, 0, 300, 12, $"DATA E HORA DA IMPRESSÃO: {vm.RodapeDataHoraImpressao}", F(6.75));
        if (!string.IsNullOrWhiteSpace(vm.RodapeSistema))
            canvas.Text(352, 0, 387, 13, vm.RodapeSistema, F(6.75), TextAlign.Right);
        canvas.AdvanceBand(h);
    }

    private static IEnumerable<string> Lines(string text) =>
        string.IsNullOrEmpty(text) ? Array.Empty<string>() : text.Split('\n');
}
