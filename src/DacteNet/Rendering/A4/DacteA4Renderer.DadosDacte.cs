using DacteNet.Rendering.Primitives;
using DacteNet.ViewModel;

namespace DacteNet.Rendering.A4;

public sealed partial class DacteA4Renderer
{
    // ------------------------------------------------------------------
    // rlb_03_DadosDACTe (dfm line 2194) - remetente/destinatário/expedidor/recebedor/tomador
    // ------------------------------------------------------------------
    private static void RenderDadosDacte(ReportCanvas canvas, DacteViewModel vm)
    {
        // Razão social / endereço are the two fields most likely to be longer than their box in real
        // CT-e's. Instead of shrinking their font (which looked inconsistent next to the rest of the
        // fixed-size form - see PR feedback), each pair of side-by-side boxes (Remetente/Destinatário,
        // then Expedidor/Recebedor, then Tomador on its own) is allowed to grow by up to one extra line
        // per field, at full size; every row below it in the same band is then pushed down by that same
        // amount so nothing overlaps. Most real documents need none of this and render pixel-identical
        // to before.
        const double pitch = 9; // raw units per extra wrapped line
        const double f675Width318 = 318, f675Width303 = 303, f675Width310 = 310, f675Width280 = 280, f675Width445 = 445;
        var fBody = F(6.75);

        int nomeLinesTop = Math.Max(
            canvas.CountWrappedLines(vm.Remetente?.RazaoSocial, f675Width318, fBody),
            canvas.CountWrappedLines(vm.Destinatario?.RazaoSocial, f675Width303, fBody));
        int endLinesTop = Math.Max(
            canvas.CountWrappedLines(vm.Remetente?.EnderecoLinha, f675Width318, fBody),
            canvas.CountWrappedLines(vm.Destinatario?.EnderecoLinha, f675Width303, fBody));
        double extraTop = (nomeLinesTop - 1 + endLinesTop - 1) * pitch;

        int nomeLinesMid = Math.Max(
            canvas.CountWrappedLines(vm.Expedidor?.RazaoSocial, f675Width318, fBody),
            canvas.CountWrappedLines(vm.Recebedor?.RazaoSocial, f675Width310, fBody));
        int endLinesMid = Math.Max(
            canvas.CountWrappedLines(vm.Expedidor?.EnderecoLinha, f675Width318, fBody),
            canvas.CountWrappedLines(vm.Recebedor?.EnderecoLinha, f675Width310, fBody));
        double extraMid = (nomeLinesMid - 1 + endLinesMid - 1) * pitch;

        int nomeLinesBottom = canvas.CountWrappedLines(vm.TomadorServico?.RazaoSocial, f675Width280, fBody);
        int endLinesBottom = canvas.CountWrappedLines(vm.TomadorServico?.EnderecoLinha, f675Width445, fBody);
        double extraBottom = (nomeLinesBottom - 1 + endLinesBottom - 1) * pitch;

        var h = 202 + extraTop + extraMid + extraBottom;
        EnsureSpace(canvas, h);
        canvas.Rect(0, 0, 741, h);

        // CFOP / natureza da operação + origem/destino da prestação (RLLabel29/rllNatOperacao,
        // RLLabel12/rllOrigPrestacao, RLLabel14/rllDestPrestacao - retrato_layout.md rlb_03_DadosDACTe,
        // NOT rlb_02_Cabecalho, which the previous implementation incorrectly placed this in)
        canvas.Text(4, 4, 115, 8, "CFOP - NATUREZA DA OPERAÇÃO", F(5.25));
        canvas.Text(4, 13, 733, 15, $"{vm.Cfop} - {vm.NaturezaOperacao}", F(8.25));
        canvas.Line(1, 26, 741, 26, 0.5); // rlsLinhaH05

        canvas.Text(4, 28, 84, 8, "ORIGEM DA PRESTAÇÃO", F(5.25));
        canvas.Text(4, 36, 360, 15, vm.MunicipioInicio, F(8.25));
        canvas.Text(374, 28, 86, 8, "DESTINO DA PRESTAÇÃO", F(5.25));
        canvas.Text(374, 36, 360, 15, vm.MunicipioFim, F(8.25));

        double yBottomTop = 109 + extraTop;
        double yBottomMid = 167 + extraTop + extraMid;
        canvas.Line(1, 51, 741, 51, 0.5);
        canvas.Line(1, yBottomTop, 741, yBottomTop, 0.5);
        canvas.Line(1, yBottomMid, 741, yBottomMid, 0.5);
        canvas.Line(370, 27, 370, yBottomMid, 0.5);

        // Remetente / Destinatário --------------------------------------------------------------
        double yNome = 54;
        double yEndereco = 65 + (nomeLinesTop - 1) * pitch;
        double yMunicipio = 76 + extraTop;
        double yCnpj = 87 + extraTop;
        double yFone = 96 + extraTop;

        canvas.Text(4, yNome, 42, 8, "REMETENTE", F(5.25));
        canvas.TextWrapped(48, yNome, 318, pitch, vm.Remetente?.RazaoSocial ?? "", fBody);
        canvas.Text(4, yEndereco, 39, 8, "ENDEREÇO", F(5.25));
        canvas.TextWrapped(48, yEndereco, 318, pitch, vm.Remetente?.EnderecoLinha ?? "", fBody);
        canvas.Text(4, yMunicipio, 38, 8, "MUNICÍPIO", F(5.25));
        canvas.Text(48, yMunicipio, 234, 19, vm.Remetente?.MunicipioUf ?? "", fBody);
        canvas.Text(282, yMunicipio, 15, 8, "CEP", fBody);
        canvas.Text(301, yMunicipio, 64, 13, vm.Remetente?.Cep ?? "", fBody);
        canvas.Text(4, yCnpj, 34, 8, "CNPJ/CPF", F(5.25));
        canvas.Text(48, yCnpj, 124, 13, vm.Remetente?.CnpjCpf ?? "", fBody);
        canvas.Text(174, yCnpj, 78, 8, "INSCRIÇÃO ESTADUAL", F(5.25));
        canvas.Text(256, yCnpj, 109, 13, vm.Remetente?.InscricaoEstadual ?? "", fBody);
        canvas.Text(262, yFone, 20, 8, "FONE", F(5.25));
        canvas.Text(288, yFone, 77, 13, vm.Remetente?.Fone ?? "", fBody);
        canvas.Text(4, yFone, 17, 8, "PAÍS", F(5.25));
        canvas.Text(48, yFone, 209, 13, vm.Remetente?.Pais ?? "", fBody);

        canvas.Text(374, yNome, 52, 8, "DESTINATÁRIO", F(5.25));
        canvas.TextWrapped(432, yNome, 303, pitch, vm.Destinatario?.RazaoSocial ?? "", fBody);
        canvas.Text(374, yEndereco, 39, 8, "ENDEREÇO", F(5.25));
        canvas.TextWrapped(432, yEndereco, 303, pitch, vm.Destinatario?.EnderecoLinha ?? "", fBody);
        canvas.Text(374, yMunicipio, 38, 8, "MUNICÍPIO", F(5.25));
        canvas.Text(432, yMunicipio, 225, 13, vm.Destinatario?.MunicipioUf ?? "", fBody);
        canvas.Text(658, yMunicipio, 15, 8, "CEP", F(5.25));
        canvas.Text(677, yMunicipio, 57, 13, vm.Destinatario?.Cep ?? "", fBody);
        canvas.Text(374, yCnpj, 34, 8, "CNPJ/CPF", F(5.25));
        canvas.Text(432, yCnpj, 115, 18, vm.Destinatario?.CnpjCpf ?? "", fBody);
        canvas.Text(551, yCnpj, 78, 8, "INSCRIÇÃO ESTADUAL", F(5.25));
        canvas.Text(632, yCnpj, 102, 13, vm.Destinatario?.InscricaoEstadual ?? "", fBody);
        canvas.Text(640, yFone, 20, 8, "FONE", F(5.25));
        canvas.Text(664, yFone, 70, 13, vm.Destinatario?.Fone ?? "", fBody);
        canvas.Text(374, yFone, 17, 8, "PAÍS", F(5.25));
        canvas.Text(432, yFone, 203, 13, vm.Destinatario?.Pais ?? "", fBody);

        // Expedidor / Recebedor -------------------------------------------------------------------
        double yNome2 = 111 + extraTop;
        double yEndereco2 = 119 + extraTop + (nomeLinesMid - 1) * pitch;
        double yMunicipio2 = 135 + extraTop + extraMid;
        double yCnpj2 = 144 + extraTop + extraMid;
        double yFone2 = 153 + extraTop + extraMid;

        canvas.Text(4, yNome2, 41, 8, "EXPEDIDOR", F(5.25));
        canvas.TextWrapped(48, yNome2, 318, pitch, vm.Expedidor?.RazaoSocial ?? "", fBody);
        canvas.Text(4, yEndereco2, 39, 8, "ENDEREÇO", F(5.25));
        canvas.TextWrapped(48, yEndereco2, 318, pitch, vm.Expedidor?.EnderecoLinha ?? "", fBody);
        canvas.Text(4, yMunicipio2, 38, 8, "MUNICÍPIO", F(5.25));
        canvas.Text(48, yMunicipio2, 234, 13, vm.Expedidor?.MunicipioUf ?? "", fBody);
        canvas.Text(284, yMunicipio2, 15, 8, "CEP", F(5.25));
        canvas.Text(301, yMunicipio2, 64, 13, vm.Expedidor?.Cep ?? "", fBody);
        canvas.Text(4, yCnpj2, 34, 8, "CNPJ/CPF", F(5.25));
        canvas.Text(48, yCnpj2, 124, 13, vm.Expedidor?.CnpjCpf ?? "", fBody);
        canvas.Text(174, yCnpj2, 78, 8, "INSCRIÇÃO ESTADUAL", F(5.25));
        canvas.Text(256, yCnpj2, 109, 13, vm.Expedidor?.InscricaoEstadual ?? "", fBody);
        canvas.Text(262, yFone2, 20, 8, "FONE", F(5.25));
        canvas.Text(288, yFone2, 77, 13, vm.Expedidor?.Fone ?? "", fBody);
        canvas.Text(4, yFone2, 17, 8, "PAÍS", F(5.25));
        canvas.Text(48, yFone2, 212, 13, vm.Expedidor?.Pais ?? "", fBody);

        canvas.Text(374, yNome2, 44, 8, "RECEBEDOR", F(5.25));
        canvas.TextWrapped(424, yNome2, 310, pitch, vm.Recebedor?.RazaoSocial ?? "", fBody);
        canvas.Text(374, yEndereco2, 39, 8, "ENDEREÇO", F(5.25));
        canvas.TextWrapped(424, yEndereco2, 310, pitch, vm.Recebedor?.EnderecoLinha ?? "", fBody);
        canvas.Text(374, yMunicipio2, 38, 8, "MUNICÍPIO", F(5.25));
        canvas.Text(424, yMunicipio2, 226, 13, vm.Recebedor?.MunicipioUf ?? "", fBody);
        canvas.Text(653, yMunicipio2, 15, 8, "CEP", F(5.25));
        canvas.Text(670, yMunicipio2, 64, 13, vm.Recebedor?.Cep ?? "", fBody);
        canvas.Text(374, yCnpj2, 34, 8, "CNPJ/CPF", F(5.25));
        canvas.Text(424, yCnpj2, 121, 13, vm.Recebedor?.CnpjCpf ?? "", fBody);
        canvas.Text(551, yCnpj2, 78, 8, "INSCRIÇÃO ESTADUAL", F(5.25));
        canvas.Text(632, yCnpj2, 102, 13, vm.Recebedor?.InscricaoEstadual ?? "", fBody);
        canvas.Text(640, yFone2, 20, 8, "FONE", F(5.25));
        canvas.Text(664, yFone2, 70, 13, vm.Recebedor?.Fone ?? "", fBody);
        canvas.Text(374, yFone2, 17, 8, "PAÍS", F(5.25));
        canvas.Text(424, yFone2, 209, 13, vm.Recebedor?.Pais ?? "", fBody);

        // Tomador do serviço ----------------------------------------------------------------------
        double baseTom = extraTop + extraMid;
        double yNomeRowTom = 169 + baseTom;
        double yLabelTomTom = 170 + baseTom;
        double yEnderecoTom = 177 + baseTom + (nomeLinesBottom - 1) * pitch;
        double yLabelEnderecoTom = 178 + baseTom + (nomeLinesBottom - 1) * pitch;
        double yCnpjRowTom = 187 + baseTom + (nomeLinesBottom - 1) * pitch + (endLinesBottom - 1) * pitch;

        canvas.Text(4, yLabelTomTom, 81, 8, "TOMADOR DO SERVIÇO", F(5.25));
        canvas.TextWrapped(88, yNomeRowTom, 280, pitch, vm.TomadorServico?.RazaoSocial ?? "", fBody);
        canvas.Text(4, yLabelEnderecoTom, 39, 8, "ENDEREÇO", F(5.25));
        canvas.TextWrapped(48, yEnderecoTom, 445, pitch, vm.TomadorServico?.EnderecoLinha ?? "", fBody);
        canvas.Text(374, yLabelTomTom, 38, 8, "MUNICÍPIO", F(5.25));
        canvas.Text(416, yNomeRowTom, 233, 13, vm.TomadorServico?.MunicipioUf ?? "", fBody);
        canvas.Text(653, yLabelTomTom, 15, 8, "CEP", F(5.25));
        canvas.Text(670, yNomeRowTom, 64, 13, vm.TomadorServico?.Cep ?? "", fBody);
        canvas.Text(4, yCnpjRowTom, 34, 8, "CNPJ/CPF", F(5.25));
        canvas.Text(41, yCnpjRowTom, 130, 13, vm.TomadorServico?.CnpjCpf ?? "", fBody);
        canvas.Text(174, yCnpjRowTom, 78, 8, "INSCRIÇÃO ESTADUAL", F(5.25));
        canvas.Text(256, yCnpjRowTom, 111, 13, vm.TomadorServico?.InscricaoEstadual ?? "", fBody);
        canvas.Text(374, yCnpjRowTom, 20, 8, "FONE", F(5.25));
        canvas.Text(398, yCnpjRowTom, 85, 13, vm.TomadorServico?.Fone ?? "", fBody);
        canvas.Text(500, yLabelEnderecoTom, 17, 8, "PAÍS", F(5.25));
        canvas.Text(520, yEnderecoTom, 214, 13, vm.TomadorServico?.Pais ?? "", fBody);

        canvas.AdvanceBand(h);
    }

    // ------------------------------------------------------------------
    // rlb_03_DadosDACTe_OS (dfm line 7681) - CT-e OS / CT-e Simplificado tomador-only block
    // ------------------------------------------------------------------
    private static void RenderDadosDacteOS(ReportCanvas canvas, DacteViewModel vm)
    {
        const double pitch = 9;
        var fBody = F(6.75);
        int nomeLines = canvas.CountWrappedLines(vm.TomadorServico?.RazaoSocial, 280, fBody);
        int endLines = canvas.CountWrappedLines(vm.TomadorServico?.EnderecoLinha, 445, fBody);
        double extra = (nomeLines - 1 + endLines - 1) * pitch;

        var h = 69 + extra;
        EnsureSpace(canvas, h);
        canvas.Rect(0, 0, 741, 66 + extra);
        canvas.Line(1, 28, 741, 28, 0.5);
        canvas.Line(235, 0, 235, 29, 0.5);
        canvas.Line(500, 0, 500, 29, 0.5);

        canvas.Text(4, 3, 78, 8, "INÍCIO DA PRESTAÇÃO", F(5.25));
        canvas.Text(3, 11, 228, 15, vm.MunicipioInicio, F(6.75));
        canvas.Text(240, 3, 83, 8, "PERCURSO DO VEÍCULO", F(5.25));
        canvas.Text(238, 11, 260, 15, "", F(6.75));
        canvas.Text(504, 3, 88, 8, "TÉRMINO DA PRESTAÇÃO", F(5.25));
        canvas.Text(504, 11, 233, 15, vm.MunicipioFim, F(6.75));

        double yNomeRow = 31;
        double yLabelNomeRow = 32;
        double yEnderecoRow = 40 + (nomeLines - 1) * pitch;
        double yLabelEnderecoRow = 43 + (nomeLines - 1) * pitch;
        double yCnpjLabelRow = 55 + extra;
        double yCnpjRow = 52 + extra;

        canvas.Text(4, yLabelNomeRow, 81, 8, "TOMADOR DO SERVIÇO", F(5.25));
        canvas.TextWrapped(89, yNomeRow, 280, pitch, vm.TomadorServico?.RazaoSocial ?? "", fBody);
        canvas.Text(4, yLabelEnderecoRow, 39, 8, "ENDEREÇO", F(5.25));
        canvas.TextWrapped(48, yEnderecoRow, 445, pitch, vm.TomadorServico?.EnderecoLinha ?? "", fBody);
        canvas.Text(374, yLabelNomeRow, 38, 8, "MUNICÍPIO", F(5.25));
        canvas.Text(416, yNomeRow, 233, 13, vm.TomadorServico?.MunicipioUf ?? "", fBody);
        canvas.Text(653, yLabelNomeRow, 15, 8, "CEP", F(5.25));
        canvas.Text(673, yNomeRow, 64, 13, vm.TomadorServico?.Cep ?? "", fBody);
        canvas.Text(3, yCnpjLabelRow, 34, 8, "CNPJ/CPF", F(5.25));
        canvas.Text(40, yCnpjRow, 130, 13, vm.TomadorServico?.CnpjCpf ?? "", fBody);
        canvas.Text(178, yCnpjLabelRow, 78, 8, "INSCRIÇÃO ESTADUAL", F(5.25));
        canvas.Text(260, yCnpjRow, 111, 13, vm.TomadorServico?.InscricaoEstadual ?? "", fBody);
        canvas.Text(378, yCnpjLabelRow, 20, 8, "FONE", F(5.25));
        canvas.Text(402, yCnpjRow, 85, 13, vm.TomadorServico?.Fone ?? "", fBody);
        canvas.Text(500, yLabelEnderecoRow, 17, 8, "PAÍS", F(5.25));
        canvas.Text(520, yEnderecoRow, 214, 13, vm.TomadorServico?.Pais ?? "", fBody);

        canvas.AdvanceBand(h);
    }

    // ------------------------------------------------------------------
    // rlb_04_DadosNotaFiscal (dfm line 3656) - peso/cubagem/volumes + legacy seguro
    // ------------------------------------------------------------------
    private static void RenderDadosNotaFiscal(ReportCanvas canvas, DacteViewModel vm)
    {
        const double h = 90;
        EnsureSpace(canvas, h);
        canvas.Rect(0, 0, 741, 90);
        canvas.Line(1, 26, 741, 26, 0.5);

        canvas.Text(4, 3, 91, 8, "PRODUTO PREDOMINANTE", F(5.25));
        canvas.Text(4, 13, 275, 13, vm.ProdutoPredominante, F(6.75));
        canvas.Text(286, 3, 135, 8, "OUTRAS CARACTERÍSTICAS DA CARGA", F(5.25));
        canvas.Text(287, 13, 249, 13, vm.OutrasCaracteristicasCarga, F(6.75));
        canvas.Text(546, 3, 111, 8, "VALOR TOTAL DA MERCADORIA", F(5.25));
        canvas.Text(549, 13, 185, 13, vm.ValorTotalCarga, F(6.75, true), TextAlign.Center);

        canvas.Text(5, 29, 100, 9, "PESO BRUTO (Kg)", F(5.25), TextAlign.Center);
        canvas.Memo(5, 38, 100, Lines(vm.PesoBrutoKg), F(6.75), TextAlign.Center);
        canvas.Text(118, 29, 100, 9, "PESO BASE CÁLC. (Kg)", F(5.25), TextAlign.Center);
        canvas.Memo(118, 38, 100, Lines(vm.PesoBaseCalculoKg), F(6.75), TextAlign.Center);
        canvas.Text(232, 29, 100, 9, "PESO AFERIDO (Kg)", F(5.25), TextAlign.Center);
        canvas.Memo(232, 38, 100, Lines(vm.PesoAferidoKg), F(6.75), TextAlign.Center);
        canvas.Text(341, 29, 100, 9, "CUBAGEM (M3)", F(5.25), TextAlign.Center);
        canvas.Memo(341, 38, 100, Lines(vm.CubagemM3), F(6.75), TextAlign.Center);
        canvas.Text(456, 29, 280, 9, "QTDE. VOLUMES (Unid)", F(5.25), TextAlign.Center);
        canvas.Memo(456, 38, 280, vm.LinhasMedida.Select(l => $"{l.TipoMedida}: {l.Quantidade} {l.UnidadeMedida}"), F(6.75), TextAlign.Center);

        canvas.Line(100, 27, 100, 89, 0.5);
        canvas.Line(232, 27, 232, 89, 0.5);
        canvas.Line(341, 27, 341, 89, 0.5);
        canvas.Line(456, 27, 456, 89, 0.5);

        if (vm.SeguroLegado is not null)
        {
            canvas.Line(414, 27, 414, 89, 0.5);
            canvas.Text(416, 29, 84, 8, "NOME DA SEGURADORA", F(5.25));
            canvas.Text(417, 40, 315, 19, vm.SeguroLegado.Seguradora, F(6.75));
            canvas.Text(419, 65, 51, 8, "RESPONSÁVEL", F(5.25));
            canvas.Text(416, 73, 105, 14, vm.SeguroLegado.Responsavel, F(6.75));
            canvas.Text(526, 64, 75, 8, "NÚMERO DA APÓLICE", F(5.25));
            canvas.Text(528, 73, 99, 14, vm.SeguroLegado.Apolice, F(6.75));
            canvas.Text(632, 64, 90, 8, "NÚMERO DA AVERBAÇÃO", F(5.25));
            canvas.Text(634, 73, 99, 14, vm.SeguroLegado.Averbacao, F(6.75));
        }

        canvas.AdvanceBand(h);
    }

    private static IEnumerable<string> Lines(string text) =>
        string.IsNullOrEmpty(text) ? Array.Empty<string>() : text.Split('\n');
}
