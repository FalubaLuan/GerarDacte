using DacteNet.Rendering.Primitives;
using DacteNet.ViewModel;

namespace DacteNet.Rendering.A4;

public sealed partial class DacteA4Renderer
{
    // ------------------------------------------------------------------
    // rlb_Fluxo_Carga (dfm line 7525) - air modal only, versao>=3.00, modelo<>67
    // ------------------------------------------------------------------
    private static void RenderFluxoCarga(ReportCanvas canvas, DacteViewModel vm)
    {
        if (vm.FluxoOrigem is null && vm.FluxoDestino is null && vm.FluxoRota is null) return;
        const double h = 44;
        EnsureSpace(canvas, h);
        canvas.Rect(0, 0, 741, h);
        canvas.Text(6, 2, 732, 12, "PREVISÃO DO FLUXO DA CARGA", F(6.75), TextAlign.Center);
        canvas.Line(1, 14, 741, 14, 0.5);
        canvas.Line(241, 15, 241, 42, 0.5);
        canvas.Line(494, 15, 494, 42, 0.5);
        canvas.Text(2, 17, 234, 8, "SIGLA/CÓD. INT. DA FILIAL/PORTO/ESTAÇÃO/AEROPORTO DE ORIGEM", F(5.25));
        canvas.Text(246, 16, 244, 8, "SIGLA/CÓD. INT. DA FILIAL/PORTO/ESTAÇÃO/AEROPORTO DE PASSAGEM", F(5.25));
        canvas.Text(496, 17, 236, 8, "SIGLA/CÓD. INT. DA FILIAL/PORTO/ESTAÇÃO/AEROPORTO DE DESTINO", F(5.25));
        canvas.Text(8, 26, 59, 12, vm.FluxoOrigem ?? "", F(6.75));
        canvas.Text(248, 26, 188, 12, vm.FluxoRota ?? "", F(6.75));
        canvas.Text(502, 26, 62, 12, vm.FluxoDestino ?? "", F(6.75));

        canvas.AdvanceBand(h);
    }

    // ------------------------------------------------------------------
    // rlb_09_Obs (dfm line 543) - observações + status/homologação watermark
    // ------------------------------------------------------------------
    private static void RenderObservacoes(ReportCanvas canvas, DacteViewModel vm)
    {
        var linhas = vm.LinhasObservacoes;
        double obsHeight = Math.Max(linhas.Count, 4) * 8 + 5;
        double h = Math.Max(obsHeight + 25, 68);
        EnsureSpace(canvas, h);
        canvas.Rect(0, 0, 741, h);
        canvas.Text(6, 4, 732, 13, "OBSERVAÇÕES", F(6.75), TextAlign.Center);
        canvas.Line(1, 18, 741, 18, 0.5);
        canvas.Memo(5, 19, 730, linhas, F(6));

        if (!string.IsNullOrWhiteSpace(vm.MensagemStatus))
            canvas.Text(50, 29, 640, 26, vm.MensagemStatus, F(18, true), TextAlign.Center, PdfColor.Gray);

        canvas.AdvanceBand(h);
    }

    // ------------------------------------------------------------------
    // rlb_16_DadosExcEmitente (dfm line 4367) - uso exclusivo do emissor / reservado ao fisco
    // ------------------------------------------------------------------
    private static void RenderDadosExcEmitente(ReportCanvas canvas, DacteViewModel vm)
    {
        bool hasContent = vm.ObservacoesContribuinte.Count > 0 || vm.ObservacoesFisco.Count > 0;
        double bodyHeight = hasContent
            ? Math.Max(vm.ObservacoesContribuinte.Count, vm.ObservacoesFisco.Count) * 8 + 20
            : 49;
        double h = 17 + bodyHeight;
        EnsureSpace(canvas, h);
        canvas.Rect(0, 0, 741, h);
        canvas.Text(142, 4, 171, 12, "USO EXCLUSIVO DO EMISSOR DO CT-E", F(6.75));
        canvas.Text(566, 4, 102, 12, "RESERVADO AO FISCO", F(6.75));
        canvas.Line(1, 15, 741, 15, 0.5);
        canvas.Line(500, 1, 500, h - 1, 0.5);
        canvas.Memo(5, 17, 492, vm.ObservacoesContribuinte, F(6.75));
        canvas.Memo(509, 17, 228, vm.ObservacoesFisco, F(6.75));

        canvas.AdvanceBand(h);
    }
}
