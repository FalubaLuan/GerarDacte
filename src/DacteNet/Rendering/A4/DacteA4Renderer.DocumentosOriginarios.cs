using DacteNet.Rendering.Primitives;
using DacteNet.ViewModel;

namespace DacteNet.Rendering.A4;

public sealed partial class DacteA4Renderer
{
    // ------------------------------------------------------------------
    // rlb_07_HeaderItens (dfm line 335) - "Documentos Originários", the only band with real
    // pagination in the original report. Reproduces the quoted rlb_07_HeaderItensBeforePrint /
    // AfterPrint algorithm (retrato_layout.md §6): up to 10 rows on page 1, then a manual page
    // break, then up to 58 rows per subsequent page, repeating until every row has been printed.
    // ------------------------------------------------------------------
    private static void RenderDocumentosOriginarios(ReportCanvas canvas, DacteViewModel vm)
    {
        var linhas = vm.LinhasDocumentosOriginarios;
        if (linhas.Count == 0) return;

        int index = 0;
        bool firstPage = true;
        while (index < linhas.Count)
        {
            int maxThisPage = firstPage ? 10 : 58;
            int end = Math.Min(index + maxThisPage, linhas.Count);
            var pageRows = linhas.Skip(index).Take(end - index).ToList();

            double bodyHeight = Math.Max(pageRows.Count * 12 + 10, 50);
            double h = 27 + bodyHeight;
            EnsureSpace(canvas, h);

            canvas.Rect(0, 0, 741, h);
            canvas.Text(6, 2, 732, 12, "DOCUMENTOS ORIGINÁRIOS", F(6.75), TextAlign.Center);
            canvas.Line(1, 14, 741, 14, 0.5);
            canvas.Line(370, 14, 370, h - 2, 0.5);
            canvas.Text(5, 17, 29, 8, "TP DOC.", F(5.25));
            canvas.Text(52, 17, 69, 8, "NÚM. NOTA", F(5.25));
            canvas.Text(174, 17, 86, 8, "SÉRIE/NRO. DOCUMENTO", F(5.25));
            canvas.Text(373, 17, 29, 8, "TP DOC.", F(5.25));
            canvas.Text(420, 17, 69, 8, "NÚM. NOTA", F(5.25));
            canvas.Text(542, 17, 86, 8, "SÉRIE/NRO. DOCUMENTO", F(5.25));

            canvas.Memo(5, 27, 363, pageRows.Select(r => $"{r.Item1,-14}{r.Item2}"), F(6.75));
            canvas.Memo(373, 27, 363, pageRows.Select(r => string.IsNullOrEmpty(r.Item3) ? "" : $"{r.Item3,-8}{r.Item4}"), F(6.75));

            canvas.AdvanceBand(h);

            index = end;
            firstPage = false;
            if (index < linhas.Count) canvas.NewPage();
        }
    }

    // ------------------------------------------------------------------
    // rlb_Cte_Anulado_Substituido (dfm line 8336)
    // ------------------------------------------------------------------
    private static void RenderAnuladoSubstituido(ReportCanvas canvas, DacteViewModel vm)
    {
        if (!vm.MostrarAnuladoSubstituido) return;
        const double h = 81;
        EnsureSpace(canvas, h);
        canvas.Rect(0, 0, 741, h);
        canvas.Text(6, 2, 732, 12, "CT-e  ANULADO / SUBSTITUÍDO", F(6.75), TextAlign.Center);
        canvas.Line(1, 14, 741, 14, 0.5);
        canvas.Line(370, 14, 370, 80, 0.5);
        canvas.Text(5, 17, 90, 8, vm.RotuloChaveAnuladoSubstituido, F(5.25));
        canvas.Text(5, 27, 363, 24, vm.ChaveAnuladoSubstituido, F(6.75));

        if (vm.MostrarChaveAnulacaoSubstituicao)
        {
            canvas.Text(373, 17, 84, 8, "CHAVE CT-E ANULAÇÃO", F(5.25));
            canvas.Text(373, 27, 363, 24, vm.ChaveAnulacaoSubstituicao, F(6.75));
        }

        canvas.AdvanceBand(h);
    }
}
