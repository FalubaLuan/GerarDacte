using DacteNet.Rendering.Primitives;
using DacteNet.ViewModel;

namespace DacteNet.Rendering.A4;

/// <summary>
/// Renders a <see cref="DacteViewModel"/> onto an A4 page, reproducing (band by band, in the exact
/// print order documented in analysis/retrato_layout.md §2) the layout of ACBrCTeDACTeRLRetrato.pas
/// (the "TfrmDACTeRLRetrato" Fortes Report Lite form). Coordinates passed to <see cref="ReportCanvas"/>
/// are the *same raw design units* used throughout retrato_layout.md's component tables (1 raw unit =
/// 0.75pt, see <see cref="Layout"/>), so each drawing call below can be checked directly against a row
/// of that table.
///
/// Structural note carried over from the original: every band in the source report is a Fortes
/// "header" band (there is no real repeating detail band) - the whole page is a fixed sequence of
/// bands shown/hidden by resizing to Height=0, and the *only* band with real multi-page pagination is
/// "Documentos Originários" (see <see cref="RenderDocumentosOriginarios"/>). This renderer mirrors that:
/// every band below is drawn once, top to bottom, and <see cref="EnsureSpace"/> is only a safety-net
/// page break for the (atypical) case where a CT-e's combined content does not fit on one page - the
/// original's own pagination guards (`PrintIt:=PageNumber=1`) mean that in ACBr itself this essentially
/// never needs to happen except for the documentos-originários overflow.
///
/// Known simplifications (see docs/limitations.md for the complete list):
///  - Purely decorative grid/divider lines (<c>TRLDraw</c> without any runtime logic) are reproduced for
///    the primary borders and column dividers of each band, but not exhaustively for every minor
///    hairline in the very dense vale-pedágio/tabular bands - the data itself is always positioned
///    faithfully; only some non-load-bearing ruling lines are pragmatically omitted.
///  - The alternate "barra" (strip) canhoto layout (<see cref="DacteOptions.LayoutCanhoto"/> =
///    <see cref="LayoutCanhoto.Barra"/>) is not implemented separately; the standard canhoto layout is
///    used regardless of this option.
/// </summary>
public sealed partial class DacteA4Renderer
{
    private const double PageWidthMm = 210.0;
    private const double PageHeightMm = 297.0;
    private const double MarginMm = 6.88; // matches raw Left=26 => 26*25.4/96 = 6.88mm, see retrato_layout.md §1

    /// <summary>Times New Roman is used throughout the original; DacteFonts.TimesNewRoman maps it onto the closest standard-14 font (Times-Roman/Times-Bold).</summary>
    private static DacteFont F(double sizePt, bool bold = false) => DacteFont.TimesNewRoman(sizePt, bold);

    public PdfDocument Render(DacteViewModel vm)
    {
        var doc = new PdfDocument();
        double marginPt = Layout.PtFromMm(MarginMm);
        var canvas = new ReportCanvas(doc, Layout.PtFromMm(PageWidthMm), Layout.PtFromMm(PageHeightMm),
            marginPt, marginPt, marginPt, marginPt);

        RenderDivisaoRecibo(canvas);
        RenderRecibo(canvas, vm);
        RenderCabecalho(canvas, vm);

        if (vm.ModeloOS) RenderDadosDacteOS(canvas, vm);
        else RenderDadosDacte(canvas, vm);

        if (!vm.ModeloOS) RenderDadosNotaFiscal(canvas, vm);
        RenderComplemento(canvas, vm);
        RenderProdutosPerigosos(canvas, vm);
        if (vm.ModeloOS) RenderCTeOSPrestacaoServico(canvas, vm);
        RenderValorPrestacao(canvas, vm);
        if (!vm.ModeloOS) RenderDocumentosOriginarios(canvas, vm);
        RenderAnuladoSubstituido(canvas, vm);
        RenderFluxoCarga(canvas, vm);
        RenderObservacoes(canvas, vm);
        RenderVeiculosNovos(canvas, vm);
        RenderDadosSeguradora(canvas, vm);
        RenderModaisEspecificos(canvas, vm);
        RenderDadosExcEmitente(canvas, vm);
        RenderRodapeSistema(canvas, vm);

        return doc;
    }

    /// <summary>Forces a new page if the next band would not fit in the remaining space on the current one.</summary>
    private static void EnsureSpace(ReportCanvas canvas, double heightRl)
    {
        if (canvas.RemainingHeightPt < Layout.Pt(heightRl)) canvas.NewPage();
    }

    // ------------------------------------------------------------------
    // rlb_DivisaoRecibo (dfm line ~8625) - thin divider between canhoto and body
    // ------------------------------------------------------------------
    private static void RenderDivisaoRecibo(ReportCanvas canvas)
    {
        const double h = 12;
        canvas.Line(0, 5, 741, 5, 0.5);
        canvas.AdvanceBand(h);
    }

    // ------------------------------------------------------------------
    // rlb_02_Cabecalho (dfm line 623) - main header
    // ------------------------------------------------------------------
    private static void RenderCabecalho(ReportCanvas canvas, DacteViewModel vm)
    {
        const double h = 184;
        EnsureSpace(canvas, h);
        canvas.Rect(0, 2, 741, 183);

        // --- left block: logo/issuer name+address ---
        canvas.Text(7, 10, 305, 19, vm.Emitente.RazaoSocial, F(9, true), TextAlign.Center);
        canvas.Memo(7, 34, 305, vm.Emitente.LinhasEnderecoCompleto, F(6), lineHeightRl: 9);
        canvas.Line(313, 2, 313, 184, 0.5);

        // --- title block ---
        canvas.Text(317, 4, 298, 14, vm.TituloDacte, F(9, true), TextAlign.Center);
        canvas.Text(317, 19, 298, 13, vm.SubtituloDacte, F(6, true), TextAlign.Center);
        canvas.Line(313, 32, 741, 32, 0.5);

        canvas.Text(640, 5, 76, 13, "MODAL", F(8.25, true), TextAlign.Center);
        canvas.Text(627, 18, 104, 14, vm.Modal, F(8.25, true), TextAlign.Center);
        canvas.Line(616, 2, 616, 33, 0.5);

        canvas.Text(314, 34, 32, 8, "MODELO", F(5.25));
        canvas.Text(315, 42, 30, 15, vm.ModeloOS ? "67" : "57", F(8.25, true), TextAlign.Center);
        canvas.Text(351, 34, 21, 8, "SÉRIE", F(5.25));
        canvas.Text(352, 42, 20, 15, vm.Serie, F(8.25, true), TextAlign.Center);
        canvas.Text(378, 34, 83, 9, "NÚMERO", F(5.25), TextAlign.Center);
        canvas.Text(378, 42, 83, 15, vm.NumeroCTe, F(9.75, true));
        canvas.Text(510, 34, 95, 9, "DATA E HORA DE EMISSÃO", F(5.25));
        canvas.Text(620, 34, 95, 9, "INSC. SUFRAMA DO DESTINATÁRIO", F(5.25));
        canvas.Text(510, 42, 58, 13, vm.DataHoraEmissao, F(8.25, true));
        canvas.Line(313, 57, 741, 57, 0.5); // rlsLinhaH02 (313,57,428,1)

        // --- TIPO DO CT-E / TIPO DO SERVIÇO / indicador-tomador / forma-pagamento block ---
        // (retrato_layout.md lines 234-239/250-251: RLLabel200/199 static, rllTipoCte/rllTipoServico
        // at raw Y=127/137; RLLabel28/rllTomaServico and RLLabel78/rllFormaPagamento at Y=156/166 -
        // NOT the CFOP/Origem/Destino block, which belongs to rlb_03_DadosDACTe instead.)
        canvas.Text(4, 127, 46, 8, "TIPO DO CT-E", F(5.25));
        canvas.Text(4, 137, 168, 15, vm.TipoCteTexto, F(6.75));
        canvas.Text(178, 127, 61, 8, "TIPO DO SERVIÇO", F(5.25));
        canvas.Text(178, 137, 132, 15, vm.TipoServicoTexto, F(6.75));
        canvas.Line(0, 120, vm.MostrarQrCode ? 620 : 741, 120, 1);
        canvas.Line(0, 150, vm.MostrarQrCode ? 620 : 741, 150, 1); 
        canvas.Line(176, 120, 176, vm.ModeloOS ? 151 : 180, 0.5); // rlsLinhaV01 (176,120,1,60; Height:=31 when modelo=67)

        canvas.Text(4, 156, 81, 8, vm.RotuloTomaServicoIndicador, F(5.25));
        canvas.Text(4, 166, 170, 15, vm.TomaServicoIndicador, F(6.75));
        canvas.Text(178, 156, 83, 8, vm.RotuloFormaPagamento, F(5.25));
        canvas.Text(178, 166, 134, 15, vm.ObservacaoFormaPagamento, F(6.75));

        canvas.Barcode128(316, 62, vm.MostrarQrCode ? 298 : 419, 26, vm.BarcodeDigitos);

        if (vm.MostrarQrCode && !string.IsNullOrWhiteSpace(vm.QrCodeUrl))
        {
            canvas.QrCode(630, 70, 94, vm.QrCodeUrl!);
            canvas.Line(620, 58, 620, 183, 1);
        }

        canvas.Text(316, 92, 58, 11, "Chave de acesso", F(6, true));
        canvas.Text(315, 104, 300, 14, vm.ChaveAcessoFormatada, F(8.25, true));

        canvas.Text(334, 156, 55, 8, "N. PROTOCOLO", F(5.25, true));
        canvas.Text(336, 166, 300, 15, vm.TextoProtocolo, F(8.25, true), TextAlign.Center);

        if (!vm.UsarBarcodeContingencia)
        {
            canvas.Text(316, 122, 298, 13, "Válida somente após o pagamento do imposto e trânsito com o Documento", F(6.75, true), TextAlign.Center);
            canvas.Text(316, 134, 298, 13, "Autorizadora, ou em http://www.cte.fazenda.gov.br/portal", F(6.75, true), TextAlign.Center);
        }

        canvas.AdvanceBand(h);
    }

    // ------------------------------------------------------------------
    // rlb_17_Sistema (dfm line 4320) - footer strip
    // ------------------------------------------------------------------
    private static void RenderRodapeSistema(ReportCanvas canvas, DacteViewModel vm)
    {
        const double h = 16;
        EnsureSpace(canvas, h);
        if (vm.RodapeDataHoraImpressao is not null)
            canvas.Text(2, 0, 300, 12, $"DATA E HORA DA IMPRESSÃO: {vm.RodapeDataHoraImpressao}", F(6.75));
        if (!string.IsNullOrWhiteSpace(vm.RodapeSistema))
            canvas.Text(352, 0, 387, 13, vm.RodapeSistema, F(6.75), TextAlign.Right);
        canvas.AdvanceBand(h);
    }
}
