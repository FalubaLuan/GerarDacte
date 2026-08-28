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
        const double h = 202;
        EnsureSpace(canvas, h);
        canvas.Rect(0, 0, 741, 202);

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

        canvas.Line(1, 51, 741, 51, 0.5);
        canvas.Line(1, 109, 741, 109, 0.5);
        canvas.Line(1, 167, 741, 167, 0.5);
        canvas.Line(370, 27, 370, 168, 0.5);

        // Remetente
        canvas.Text(4, 54, 42, 8, "REMETENTE", F(5.25));
        canvas.Text(48, 54, 318, 13, vm.Remetente?.RazaoSocial ?? "", F(6.75));
        canvas.Text(4, 65, 39, 8, "ENDEREÇO", F(5.25));
        var enderecoRem = vm.Remetente?.EnderecoLinha ?? "";
        canvas.Text(48, 65, 318, 13, enderecoRem.Length > 65 ? $"{enderecoRem[..65]}..." : enderecoRem, F(6.75));
        canvas.Text(4, 76, 38, 8, "MUNICÍPIO", F(5.25));
        canvas.Text(48, 76, 234, 19, vm.Remetente?.MunicipioUf ?? "", F(6.75));
        canvas.Text(282, 76, 15, 8, "CEP", F(6.75));
        canvas.Text(301, 76, 64, 13, vm.Remetente?.Cep ?? "", F(6.75));
        canvas.Text(4, 87, 34, 8, "CNPJ/CPF", F(5.25));
        canvas.Text(48, 87, 124, 13, vm.Remetente?.CnpjCpf ?? "", F(6.75));
        canvas.Text(174, 87, 78, 8, "INSCRIÇÃO ESTADUAL", F(5.25));
        canvas.Text(256, 87, 109, 13, vm.Remetente?.InscricaoEstadual ?? "", F(6.75));
        canvas.Text(262, 96, 20, 8, "FONE", F(5.25));
        canvas.Text(288, 96, 77, 13, vm.Remetente?.Fone ?? "", F(6.75));
        canvas.Text(4, 96, 17, 8, "PAÍS", F(5.25));
        canvas.Text(48, 96, 209, 13, vm.Remetente?.Pais ?? "", F(6.75));

        // Destinatário
        canvas.Text(374, 54, 52, 8, "DESTINATÁRIO", F(5.25));
        canvas.Text(432, 54, 303, 13, vm.Destinatario?.RazaoSocial ?? "", F(6.75));
        canvas.Text(374, 65, 39, 8, "ENDEREÇO", F(5.25));
        var enderecoDest = vm.Destinatario?.EnderecoLinha ?? "";
        canvas.Text(432, 65, 303, 10, enderecoDest.Length > 65 ? $"{enderecoDest[..65]}..." : enderecoDest, F(6.75));
        canvas.Text(374, 76, 38, 8, "MUNICÍPIO", F(5.25));
        canvas.Text(432, 76, 225, 13, vm.Destinatario?.MunicipioUf ?? "", F(6.75));
        canvas.Text(658, 76, 15, 8, "CEP", F(6.75));
        canvas.Text(677, 76, 57, 13, vm.Destinatario?.Cep ?? "", F(6.75));
        canvas.Text(374, 87, 34, 8, "CNPJ/CPF", F(5.25));
        canvas.Text(432, 87, 115, 18, vm.Destinatario?.CnpjCpf ?? "", F(6.75));
        canvas.Text(551, 87, 78, 8, "INSCRIÇÃO ESTADUAL", F(5.25));
        canvas.Text(632, 87, 102, 13, vm.Destinatario?.InscricaoEstadual ?? "", F(6.75));
        canvas.Text(640, 96, 20, 8, "FONE", F(5.25));
        canvas.Text(664, 96, 70, 13, vm.Destinatario?.Fone ?? "", F(6.75));
        canvas.Text(374, 96, 17, 8, "PAÍS", F(5.25));
        canvas.Text(432, 96, 203, 13, vm.Destinatario?.Pais ?? "", F(6.75));

        // Expedidor
        canvas.Text(4, 111, 41, 8, "EXPEDIDOR", F(5.25));
        canvas.Text(48, 111, 318, 13, vm.Expedidor?.RazaoSocial ?? "", F(6.75));
        canvas.Text(4, 119, 39, 8, "ENDEREÇO", F(5.25));
        canvas.Text(48, 119, 318, 13, vm.Expedidor?.EnderecoLinha ?? "", F(6.75));
        canvas.Text(4, 135, 38, 8, "MUNICÍPIO", F(5.25));
        canvas.Text(48, 135, 234, 13, vm.Expedidor?.MunicipioUf ?? "", F(6.75));
        canvas.Text(284, 135, 15, 8, "CEP", F(5.25));
        canvas.Text(301, 135, 64, 13, vm.Expedidor?.Cep ?? "", F(6.75));
        canvas.Text(4, 144, 34, 8, "CNPJ/CPF", F(5.25));
        canvas.Text(48, 144, 124, 13, vm.Expedidor?.CnpjCpf ?? "", F(6.75));
        canvas.Text(174, 144, 78, 8, "INSCRIÇÃO ESTADUAL", F(5.25));
        canvas.Text(256, 144, 109, 13, vm.Expedidor?.InscricaoEstadual ?? "", F(6.75));
        canvas.Text(262, 153, 20, 8, "FONE", F(5.25));
        canvas.Text(288, 153, 77, 13, vm.Expedidor?.Fone ?? "", F(6.75));
        canvas.Text(4, 153, 17, 8, "PAÍS", F(5.25));
        canvas.Text(48, 153, 212, 13, vm.Expedidor?.Pais ?? "", F(6.75));

        // Recebedor
        canvas.Text(374, 111, 44, 8, "RECEBEDOR", F(5.25));
        canvas.Text(424, 111, 310, 15, vm.Recebedor?.RazaoSocial ?? "", F(6.75));
        canvas.Text(374, 119, 39, 8, "ENDEREÇO", F(5.25));
        canvas.Text(424, 119, 310, 13, vm.Recebedor?.EnderecoLinha ?? "", F(6.75));
        canvas.Text(374, 135, 38, 8, "MUNICÍPIO", F(5.25));
        canvas.Text(424, 135, 226, 13, vm.Recebedor?.MunicipioUf ?? "", F(6.75));
        canvas.Text(653, 135, 15, 8, "CEP", F(5.25));
        canvas.Text(670, 135, 64, 13, vm.Recebedor?.Cep ?? "", F(6.75));
        canvas.Text(374, 144, 34, 8, "CNPJ/CPF", F(5.25));
        canvas.Text(424, 144, 121, 13, vm.Recebedor?.CnpjCpf ?? "", F(6.75));
        canvas.Text(551, 144, 78, 8, "INSCRIÇÃO ESTADUAL", F(5.25));
        canvas.Text(632, 144, 102, 13, vm.Recebedor?.InscricaoEstadual ?? "", F(6.75));
        canvas.Text(640, 153, 20, 8, "FONE", F(5.25));
        canvas.Text(664, 153, 70, 13, vm.Recebedor?.Fone ?? "", F(6.75));
        canvas.Text(374, 153, 17, 8, "PAÍS", F(5.25));
        canvas.Text(424, 153, 209, 13, vm.Recebedor?.Pais ?? "", F(6.75));

        // Tomador do serviço
        canvas.Text(4, 170, 81, 8, "TOMADOR DO SERVIÇO", F(5.25));
        canvas.Text(88, 169, 280, 13, vm.TomadorServico?.RazaoSocial ?? "", F(6.75));
        canvas.Text(4, 178, 39, 8, "ENDEREÇO", F(5.25));
        canvas.Text(48, 177, 445, 13, vm.TomadorServico?.EnderecoLinha ?? "", F(6.75));
        canvas.Text(374, 170, 38, 8, "MUNICÍPIO", F(5.25));
        canvas.Text(416, 169, 233, 13, vm.TomadorServico?.MunicipioUf ?? "", F(6.75));
        canvas.Text(653, 170, 15, 8, "CEP", F(5.25));
        canvas.Text(670, 169, 64, 13, vm.TomadorServico?.Cep ?? "", F(6.75));
        canvas.Text(4, 187, 34, 8, "CNPJ/CPF", F(5.25));
        canvas.Text(41, 187, 130, 13, vm.TomadorServico?.CnpjCpf ?? "", F(6.75));
        canvas.Text(174, 187, 78, 8, "INSCRIÇÃO ESTADUAL", F(5.25));
        canvas.Text(256, 187, 111, 13, vm.TomadorServico?.InscricaoEstadual ?? "", F(6.75));
        canvas.Text(374, 187, 20, 8, "FONE", F(5.25));
        canvas.Text(398, 187, 85, 13, vm.TomadorServico?.Fone ?? "", F(6.75));
        canvas.Text(500, 178, 17, 8, "PAÍS", F(5.25));
        canvas.Text(520, 177, 214, 13, vm.TomadorServico?.Pais ?? "", F(6.75));

        canvas.AdvanceBand(h);
    }

    // ------------------------------------------------------------------
    // rlb_03_DadosDACTe_OS (dfm line 7681) - CT-e OS / CT-e Simplificado tomador-only block
    // ------------------------------------------------------------------
    private static void RenderDadosDacteOS(ReportCanvas canvas, DacteViewModel vm)
    {
        const double h = 69;
        EnsureSpace(canvas, h);
        canvas.Rect(0, 0, 741, 66);
        canvas.Line(1, 28, 741, 28, 0.5);
        canvas.Line(235, 0, 235, 29, 0.5);
        canvas.Line(500, 0, 500, 29, 0.5);

        canvas.Text(4, 3, 78, 8, "INÍCIO DA PRESTAÇÃO", F(5.25));
        canvas.Text(3, 11, 228, 15, vm.MunicipioInicio, F(6.75));
        canvas.Text(240, 3, 83, 8, "PERCURSO DO VEÍCULO", F(5.25));
        canvas.Text(238, 11, 260, 15, "", F(6.75));
        canvas.Text(504, 3, 88, 8, "TÉRMINO DA PRESTAÇÃO", F(5.25));
        canvas.Text(504, 11, 233, 15, vm.MunicipioFim, F(6.75));

        canvas.Text(4, 32, 81, 8, "TOMADOR DO SERVIÇO", F(5.25));
        canvas.Text(89, 31, 280, 13, vm.TomadorServico?.RazaoSocial ?? "", F(6.75));
        canvas.Text(4, 43, 39, 8, "ENDEREÇO", F(5.25));
        canvas.Text(48, 40, 445, 13, vm.TomadorServico?.EnderecoLinha ?? "", F(6.75));
        canvas.Text(374, 32, 38, 8, "MUNICÍPIO", F(5.25));
        canvas.Text(416, 31, 233, 13, vm.TomadorServico?.MunicipioUf ?? "", F(6.75));
        canvas.Text(653, 32, 15, 8, "CEP", F(5.25));
        canvas.Text(673, 31, 64, 13, vm.TomadorServico?.Cep ?? "", F(6.75));
        canvas.Text(3, 55, 34, 8, "CNPJ/CPF", F(5.25));
        canvas.Text(40, 52, 130, 13, vm.TomadorServico?.CnpjCpf ?? "", F(6.75));
        canvas.Text(178, 55, 78, 8, "INSCRIÇÃO ESTADUAL", F(5.25));
        canvas.Text(260, 52, 111, 13, vm.TomadorServico?.InscricaoEstadual ?? "", F(6.75));
        canvas.Text(378, 55, 20, 8, "FONE", F(5.25));
        canvas.Text(402, 52, 85, 13, vm.TomadorServico?.Fone ?? "", F(6.75));
        canvas.Text(500, 43, 17, 8, "PAÍS", F(5.25));
        canvas.Text(520, 40, 214, 13, vm.TomadorServico?.Pais ?? "", F(6.75));

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
