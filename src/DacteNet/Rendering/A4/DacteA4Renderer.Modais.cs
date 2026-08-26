using DacteNet.Rendering.Primitives;
using DacteNet.ViewModel;

namespace DacteNet.Rendering.A4;

public sealed partial class DacteA4Renderer
{
    // ------------------------------------------------------------------
    // rlb_Dados_Seguradora (dfm line 8074) - v>=3.00 insurance block, modelo=67 or modal=Multimodal
    // ------------------------------------------------------------------
    private static void RenderDadosSeguradora(ReportCanvas canvas, DacteViewModel vm)
    {
        if (vm.SegurosModernos.Count == 0) return;
        const double h = 44;
        EnsureSpace(canvas, h);
        canvas.Rect(0, 0, 741, h);
        canvas.Text(6, 2, 732, 13, "SEGURO DA VIAGEM", F(6.75), TextAlign.Center);
        canvas.Line(1, 15, 741, 15, 0.5);
        canvas.Line(246, 15, 246, 43, 0.5);
        canvas.Line(492, 16, 492, 44, 0.5);
        canvas.Text(8, 17, 51, 8, "RESPONSÁVEL", F(5.25));
        canvas.Text(252, 17, 84, 8, "NOME DA SEGURADORA", F(5.25));
        canvas.Text(500, 17, 75, 8, "NÚMERO DA APÓLICE", F(5.25));

        canvas.Memo(9, 25, 224, vm.SegurosModernos.Select(s => s.Responsavel), F(6.75));
        canvas.Memo(252, 25, 224, vm.SegurosModernos.Select(s => s.Seguradora), F(6.75));
        canvas.Memo(501, 25, 224, vm.SegurosModernos.Select(s => s.Apolice), F(6.75));

        canvas.AdvanceBand(h);
    }

    /// <summary>Dispatches to whichever single modal-specific block applies (mutually exclusive per Ide.Modal).</summary>
    private static void RenderModaisEspecificos(ReportCanvas canvas, DacteViewModel vm)
    {
        if (vm.ModalRodoviario is not null) RenderModalRodoviario(canvas, vm.ModalRodoviario);
        else if (vm.ModalAereo is not null) RenderModalAereo(canvas, vm.ModalAereo);
        else if (vm.ModalAquaviario is not null) RenderModalAquaviario(canvas, vm.ModalAquaviario);
        // Ferroviário/Dutoviário: no modal-specific section in this layout (retrato_layout.md §7).
    }

    // ------------------------------------------------------------------
    // rlb_10_ModRodFracionado (dfm line 1340) + rlb_11_ModRodLot104 (dfm line 6502)
    // ------------------------------------------------------------------
    private static void RenderModalRodoviario(ReportCanvas canvas, ModalRodoviarioVm rodo)
    {
        const double h1 = 44;
        EnsureSpace(canvas, h1 + (rodo.Lotacao ? 107 : 0));
        canvas.Rect(0, 0, 741, h1);
        canvas.Line(1, 15, 741, 15, 0.5);
        canvas.Text(6, 2, 732, 13, rodo.ModeloOS ? "DADOS ESPECÍFICOS DO MODAL RODOVIÁRIO"
            : (rodo.Lotacao ? "DADOS ESPECÍFICOS DO MODAL RODOVIÁRIO - CARGA LOTAÇÃO" : "DADOS ESPECÍFICOS DO MODAL RODOVIÁRIO - CARGA FRACIONADA"),
            F(6.75), TextAlign.Center);

        canvas.Text(6, 17, 72, 8, rodo.ModeloOS ? "TERMO AUTORIZAÇÃO DE FRETAMENTO" : "RNTRC DA EMPRESA", F(5.25));
        canvas.Text(6, 25, 64, 12, rodo.RntrcOuTaf, F(6.75));

        canvas.Text(84, 17, 18, 8, rodo.ModeloOS ? "Nº DE REGISTRO ESTADUAL" : "CIOT", F(5.25));
        canvas.Text(84, 25, 32, 12, rodo.CiotOuRegistroEstadual, F(6.75));

        canvas.Text(154, 17, 35, 8, rodo.ModeloOS ? "PLACA DO VEÍCULO" : "LOTAÇÃO", F(5.25));
        canvas.Text(162, 25, 18, 12, rodo.ModeloOS ? rodo.LotacaoTexto : (rodo.Lotacao ? "SIM" : "NÃO"), F(6.75));

        canvas.Text(196, 17, 101, 8, rodo.ModeloOS ? "RENAVAM DO VEÍCULO" : "DATA PREVISTA DE ENTREGA", F(5.25));
        canvas.Text(196, 25, 72, 12, rodo.ModeloOS ? rodo.DataPrevistaEntrega : rodo.DataPrevistaEntrega, F(6.75));

        canvas.AdvanceBand(h1);

        if (!rodo.Lotacao) return;

        const double h2 = 107;
        EnsureSpace(canvas, h2);
        canvas.Rect(0, 1, 741, 104);
        canvas.Line(1, 15, 740, 15, 0.5);
        canvas.Line(1, 26, 740, 26, 0.5);
        canvas.Line(1, 79, 740, 79, 0.5);
        canvas.Line(207, 1, 207, 80, 0.5);
        canvas.Text(2, 2, 202, 12, "IDENTIFICAÇÃO DO CONJ. TRANSPORTADOR", F(6.75), TextAlign.Center);
        canvas.Text(214, 2, 524, 12, "INFORMAÇÕES REFERENTES AO VALE-PEDÁGIO", F(6.75), TextAlign.Center);
        canvas.Text(2, 17, 17, 8, "TIPO", F(5.25));
        canvas.Text(44, 17, 25, 8, "PLACA", F(5.25));
        canvas.Text(102, 17, 11, 8, "UF", F(5.25));
        canvas.Text(124, 17, 26, 8, "RNTRC", F(5.25));
        canvas.Text(210, 17, 68, 8, "CNPJ FORNECEDOR", F(5.25));
        canvas.Text(334, 17, 87, 8, "NÚMERO COMPROVANTE", F(5.25));
        canvas.Text(618, 17, 70, 8, "CNPJ RESPONSÁVEL", F(5.25));

        canvas.Memo(2, 28, 36, rodo.Veiculos.Select(v => v.Tipo), F(6.75));
        canvas.Memo(44, 28, 53, rodo.Veiculos.Select(v => v.Placa), F(6.75));
        canvas.Memo(102, 28, 16, rodo.Veiculos.Select(v => v.Uf), F(6.75));
        canvas.Memo(124, 28, 77, rodo.Veiculos.Select(v => v.Rntrc), F(6.75));
        canvas.Memo(210, 28, 117, rodo.ValesPedagio.Select(v => v.Item1), F(6.75));
        canvas.Memo(334, 28, 275, rodo.ValesPedagio.Select(v => v.Item2), F(6.75));
        canvas.Memo(618, 28, 117, rodo.ValesPedagio.Select(v => v.Item3), F(6.75));

        canvas.Text(4, 82, 76, 8, "NOME DO MOTORISTA", F(5.25));
        canvas.Text(4, 91, 76, 12, rodo.Motoristas.FirstOrDefault() ?? "", F(6.75));
        canvas.Text(351, 82, 148, 8, "IDENTIFICAÇÃO DOS LACRES EM TRÂNSITO", F(5.25));
        canvas.Text(351, 91, 41, 12, string.Join("/", rodo.Lacres), F(6.75));

        canvas.AdvanceBand(h2);
    }

    // ------------------------------------------------------------------
    // rlb_12_ModAereo (dfm line 5333)
    // ------------------------------------------------------------------
    private static void RenderModalAereo(ReportCanvas canvas, ModalAereoVm aereo)
    {
        const double h = 97;
        EnsureSpace(canvas, h);
        canvas.Rect(0, 0, 741, h);
        canvas.Line(1, 14, 740, 14, 0.5);
        canvas.Line(1, 38, 740, 38, 0.5);
        canvas.Line(1, 70, 740, 70, 0.5);
        canvas.Text(8, 1, 730, 11, "INFORMAÇÕES ESPECÍFICAS DO MODAL AÉREO", F(6.75), TextAlign.Center);

        canvas.Text(6, 16, 152, 8, "CARACTERISTICAS ADICIONAIS DO SERVIÇO", F(5.25));
        canvas.Text(262, 16, 167, 8, "CARACTERISTICAS ADICIONAIS DO TRANSPORTE", F(5.25));
        canvas.Text(543, 16, 83, 8, "NÚMERO OPERACIONAL", F(5.25));
        canvas.Text(632, 16, 105, 19, aereo.NumeroOca, F(8.25, true));

        canvas.Text(8, 40, 250, 9, "DADOS DA TARIFA", F(5.25), TextAlign.Center);
        canvas.Text(2, 50, 30, 8, "TRECHO", F(5.25));
        canvas.Text(72, 50, 11, 8, "CL", F(5.25));
        canvas.Text(95, 50, 29, 8, "CÓDIGO", F(5.25));
        canvas.Text(158, 50, 26, 8, "VALOR", F(5.25));
        canvas.Text(158, 58, 95, 13, aereo.Tarifa, F(6.75, true), TextAlign.Right);
        canvas.Text(262, 40, 65, 8, "CONTA CORRENTE", F(5.25));
        canvas.Text(262, 49, 67, 12, aereo.ContaCorrente, F(6.75));
        canvas.Text(598, 40, 73, 8, "NÚMERO DA MINUTA", F(5.25));
        canvas.Text(672, 50, 65, 19, aereo.NumeroMinuta, F(8.25, true));

        canvas.Text(2, 72, 27, 8, "RETIRA", F(5.25));
        canvas.Text(2, 81, 26, 13, aereo.RetiradaCarga, F(6.75), TextAlign.Center);
        canvas.Text(39, 72, 149, 8, "DADOS RELATIVOS A RETIRADA DA CARGA", F(5.25));
        canvas.Text(39, 80, 554, 14, aereo.DetalheRetirada, F(6.75));
        canvas.Text(598, 72, 92, 8, "LOJA OU AGENTE EMISSOR", F(5.25));
        canvas.Text(598, 81, 88, 12, "", F(6.75));

        canvas.AdvanceBand(h);
    }

    // ------------------------------------------------------------------
    // rlb_13_ModAquaviario (dfm line 5846)
    // ------------------------------------------------------------------
    private static void RenderModalAquaviario(ReportCanvas canvas, ModalAquaviarioVm aquav)
    {
        const double h = 92;
        EnsureSpace(canvas, h);
        canvas.Rect(0, 0, 741, h);
        canvas.Text(6, 2, 732, 12, "DADOS ESPECÍFICOS DO MODAL AQUAVIÁRIO", F(6.75), TextAlign.Center);
        canvas.Line(1, 15, 740, 15, 0.5);
        canvas.Line(1, 39, 740, 39, 0.5);
        canvas.Line(1, 63, 740, 63, 0.5);
        canvas.Line(402, 16, 402, 89, 0.5);

        canvas.Text(6, 17, 77, 8, "PORTO DE EMBARQUE", F(5.25));
        canvas.Text(6, 26, 71, 12, aquav.PortoEmbarque, F(6.75));
        canvas.Text(406, 17, 67, 8, "PORTO DE DESTINO", F(5.25));
        canvas.Text(406, 26, 64, 12, aquav.PortoDestino, F(6.75));

        canvas.Text(6, 41, 141, 8, "IDENTIFICAÇÃO DO NAVIO / REBOCADOR", F(5.25));
        canvas.Text(6, 50, 89, 12, aquav.Navio, F(6.75));
        canvas.Text(406, 41, 95, 8, "VR DA B. DE CALC. AFRMM", F(5.25));
        canvas.Text(406, 50, 57, 12, aquav.BaseCalculoAfrmm, F(6.75));
        canvas.Text(518, 41, 56, 8, "VLR DO AFRMM", F(5.25));
        canvas.Text(518, 50, 66, 12, aquav.ValorAfrmm, F(6.75));
        canvas.Text(614, 41, 74, 8, "TIPO DE NAVEGAÇÃO", F(5.25));
        canvas.Text(614, 50, 45, 12, aquav.TipoNavegacao, F(6.75));
        canvas.Text(694, 41, 33, 8, "DIREÇÃO", F(5.25));
        canvas.Text(694, 50, 41, 12, aquav.Direcao, F(6.75));

        canvas.Text(6, 65, 108, 8, "IDENTIFICAÇÃO DA(S) BALSA(S)", F(5.25));
        canvas.Text(6, 74, 49, 12, aquav.Balsas, F(6.75));
        canvas.Text(406, 65, 116, 8, "IDENTIFICAÇÃO DOS CONTEINERS", F(5.25));

        canvas.AdvanceBand(h);
    }
}
