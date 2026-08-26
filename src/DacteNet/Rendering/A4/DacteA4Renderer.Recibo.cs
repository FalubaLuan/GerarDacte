using DacteNet.Rendering.Primitives;
using DacteNet.ViewModel;

namespace DacteNet.Rendering.A4;

public sealed partial class DacteA4Renderer
{
    /// <summary>
    /// Standard canhoto ("recibo de entrega") declaration text. The exact wording is not quoted
    /// verbatim in retrato_layout.md (it only documents the label's position/font, not its default
    /// caption's text) - this is the standard boilerplate text used on Brazilian CT-e/DACTE receipts,
    /// used here as a documented best-effort stand-in. See docs/limitations.md.
    /// </summary>
    private const string TextoRecebemosDe =
        "Recebi(emos) os volumes constantes do Conhecimento de Transporte Eletrônico indicado ao lado, em perfeito estado, salvo disposição em contrário anotada abaixo.";

    // ------------------------------------------------------------------
    // rlb_01_Recibo (dfm line 28) / rlb_01_Recibo_Aereo - canhoto (tear-off receipt stub)
    // ------------------------------------------------------------------
    private static void RenderRecibo(ReportCanvas canvas, DacteViewModel vm)
    {
        // Canhoto suppressed for Anulação/Substituto (retrato_layout.md §4).
        if (vm.MostrarAnuladoSubstituido) return;

        if (vm.ModalAereo is not null) RenderReciboAereo(canvas, vm);
        else RenderReciboPadrao(canvas, vm);
    }

    private static void RenderReciboPadrao(ReportCanvas canvas, DacteViewModel vm)
    {
        const double h = 72;
        EnsureSpace(canvas, h);
        canvas.Rect(0, 0, 741, 71, BorderSides.Left | BorderSides.Top | BorderSides.Right);

        if (!string.IsNullOrWhiteSpace(vm.TextoResumoCanhoto))
            canvas.Text(6, 2, 732, 13, vm.TextoResumoCanhoto, F(6), TextAlign.Center);

        canvas.Text(6, 3, 732, 10, TextoRecebemosDe, F(6), TextAlign.Center);
        canvas.Line(1, 14, 740, 14, 0.5);

        canvas.Text(6, 16, 30, 12, "NOME", F(6.75));
        canvas.Text(6, 44, 15, 12, "RG", F(6.75));
        canvas.Line(203, 14, 203, 70, 0.5);
        canvas.Line(475, 14, 475, 70, 0.5);
        canvas.Line(595, 14, 595, 70, 0.5);
        canvas.Line(1, 39, 201, 39, 0.5);

        canvas.Text(481, 19, 108, 9, "CHEGADA DATA/HORA", F(5.25), TextAlign.Center);
        canvas.Text(481, 27, 108, 16, "__/__/__    __:__", F(9), TextAlign.Center);
        canvas.Text(481, 42, 108, 9, "SAÍDA DATA/HORA", F(5.25), TextAlign.Center);
        canvas.Text(481, 51, 108, 16, "__/__/__    __:__", F(9), TextAlign.Center);
        canvas.Text(207, 56, 262, 11, "ASSINATURA / CARIMBO", F(6), TextAlign.Center);

        canvas.Text(616, 37, 14, 12, "N.", F(6.75));
        canvas.Text(636, 35, 86, 16, vm.NumeroCTe, F(9.75, true));
        canvas.Text(600, 50, 30, 12, "SÉRIE:", F(6.75));
        canvas.Text(636, 49, 50, 13, vm.Serie, F(8.25, true));
        canvas.Text(647, 19, 28, 13, vm.RotuloCTe, F(8.25, true));

        canvas.AdvanceBand(h);
    }

    private static void RenderReciboAereo(ReportCanvas canvas, DacteViewModel vm)
    {
        const double h = 116;
        EnsureSpace(canvas, h);
        canvas.Rect(0, 0, 741, h - 1, BorderSides.Left | BorderSides.Top | BorderSides.Right);
        canvas.Line(1, 50, 740, 50, 0.5);
        canvas.Line(367, 61, 367, 113, 0.5);

        canvas.Memo(2, 19, 736, new[] { TextoRecebemosDe }, F(6));

        canvas.Text(121, 52, 88, 8, "EXPEDIDOR / REMETENTE", F(5.25), TextAlign.Center);
        canvas.Text(508, 52, 100, 8, "DESTINATÁRIO / RECEBEDOR", F(5.25), TextAlign.Center);
        canvas.Text(6, 62, 30, 12, "NOME", F(6.75));
        canvas.Text(374, 62, 30, 12, "NOME", F(6.75));
        canvas.Text(206, 62, 62, 12, "DATA / HORA", F(6.75));
        canvas.Text(574, 62, 62, 12, "DATA / HORA", F(6.75));
        canvas.Text(374, 94, 15, 12, "RG", F(6.75));
        canvas.Text(206, 94, 61, 12, "ASSINATURA", F(6.75));
        canvas.Text(574, 94, 61, 12, "ASSINATURA", F(6.75));

        canvas.AdvanceBand(h);
    }
}
