using DacteNet.Rendering.Primitives;
using DacteNet.ViewModel;

namespace DacteNet.Rendering.A4;

public sealed partial class DacteA4Renderer
{
    // ------------------------------------------------------------------
    // rlb_05_Complemento (dfm line 4197)
    // ------------------------------------------------------------------
    private static void RenderComplemento(ReportCanvas canvas, DacteViewModel vm)
    {
        if (vm.ChavesComplementadas.Count == 0) return;
        const double h = 81;
        EnsureSpace(canvas, h);
        canvas.Rect(0, 0, 741, 81);
        canvas.Text(6, 3, 732, 13, "CT-e COMPLEMENTADO", F(6.75), TextAlign.Center);
        canvas.Line(1, 16, 741, 16, 0.5);
        canvas.Line(372, 16, 372, 80, 0.5);
        canvas.Text(5, 19, 119, 8, "CHAVE DO CT-E COMPLEMENTADO", F(5.25));
        canvas.Text(377, 19, 119, 8, "CHAVE DO CT-E COMPLEMENTADO", F(5.25));

        var col1 = vm.ChavesComplementadas.Where((_, i) => i % 2 == 0);
        var col2 = vm.ChavesComplementadas.Where((_, i) => i % 2 == 1);
        canvas.Memo(5, 28, 361, col1, F(6.75));
        canvas.Memo(377, 28, 352, col2, F(6.75));

        canvas.AdvanceBand(h);
    }

    // ------------------------------------------------------------------
    // rlb_06_ProdutosPerigosos (dfm line 6987)
    // ------------------------------------------------------------------
    private static void RenderProdutosPerigosos(ReportCanvas canvas, DacteViewModel vm)
    {
        if (vm.ProdutosPerigosos.Count == 0) return;
        const double h = 83;
        EnsureSpace(canvas, h);
        canvas.Rect(0, 0, 741, 83);
        canvas.Text(6, 3, 732, 13, "INFORMAÇÕES SOBRE OS PRODUTOS PERIGOSOS", F(6.75), TextAlign.Center);
        canvas.Line(1, 16, 741, 16, 0.5);
        canvas.Line(1, 30, 741, 30, 0.5);
        canvas.Text(10, 19, 36, 8, "NRO. ONU", F(5.25));
        canvas.Text(83, 19, 69, 8, "NOME APROPRIADO", F(5.25));
        canvas.Text(310, 19, 145, 8, "CLASSE/SUBCLASSE E RISCO SUBSIDIÁRIO", F(5.25));
        canvas.Text(510, 19, 83, 8, "GRUPO DE EMBALAGEM", F(5.25));
        canvas.Text(625, 19, 79, 8, "QTDE TOTAL PRODUTO", F(5.25));

        canvas.Memo(0, 30, 81, vm.ProdutosPerigosos.Select(p => p.NumeroOnu), F(6.75), TextAlign.Center);
        canvas.Memo(80, 30, 221, vm.ProdutosPerigosos.Select(p => p.NomeApropriado), F(6.75));
        canvas.Memo(300, 30, 201, vm.ProdutosPerigosos.Select(p => p.ClasseRisco), F(6.75));
        canvas.Memo(500, 30, 121, vm.ProdutosPerigosos.Select(p => p.GrupoEmbalagem), F(6.75));
        canvas.Memo(620, 30, 122, vm.ProdutosPerigosos.Select(p => p.Quantidade), F(6.75));

        canvas.AdvanceBand(h);
    }

    // ------------------------------------------------------------------
    // rlb_06_VeiculosNovos (dfm line 7281)
    // ------------------------------------------------------------------
    private static void RenderVeiculosNovos(ReportCanvas canvas, DacteViewModel vm)
    {
        if (vm.VeiculosNovos.Count == 0) return;
        const double h = 63;
        EnsureSpace(canvas, h);
        canvas.Rect(0, 0, 741, 63);
        canvas.Text(6, 3, 732, 13, "INFORMAÇÕES SOBRE OS VEÍCULOS NOVOS TRANSPORTADOS", F(6.75), TextAlign.Center);
        canvas.Line(1, 16, 741, 16, 0.5);
        canvas.Text(5, 19, 27, 8, "CHASSI", F(5.25));
        canvas.Text(128, 19, 17, 8, "COR", F(5.25));
        canvas.Text(337, 19, 59, 8, "MARCA/MODELO", F(5.25));
        canvas.Text(510, 19, 78, 8, "VR. UNIT. DO VEÍCULO", F(5.25));
        canvas.Text(625, 19, 58, 8, "FRETE UNITARIO", F(5.25));

        canvas.Memo(5, 33, 116, vm.VeiculosNovos.Select(v => v.Chassi), F(6.75));
        canvas.Memo(128, 33, 201, vm.VeiculosNovos.Select(v => v.Cor), F(6.75));
        canvas.Memo(336, 33, 161, vm.VeiculosNovos.Select(v => v.Modelo), F(6.75));
        canvas.Memo(509, 33, 108, vm.VeiculosNovos.Select(v => v.ValorUnitario), F(6.75), TextAlign.Right);
        canvas.Memo(625, 33, 112, vm.VeiculosNovos.Select(v => v.ValorFrete), F(6.75), TextAlign.Right);

        canvas.AdvanceBand(h);
    }

    // ------------------------------------------------------------------
    // rlb_CTeOS_PrestacaoServico (dfm line 8240) - modelo 67 only
    // ------------------------------------------------------------------
    private static void RenderCTeOSPrestacaoServico(ReportCanvas canvas, DacteViewModel vm)
    {
        const double h = 77;
        EnsureSpace(canvas, h);
        canvas.Rect(0, 0, 741, 77);
        canvas.Text(7, 1, 732, 12, "INFORMAÇÕES DA PRESTAÇÃO DO SERVIÇO", F(6.75), TextAlign.Center);
        canvas.Line(1, 14, 741, 14, 0.5);
        canvas.Text(5, 17, 47, 8, "QUANTIDADE", F(5.25));
        canvas.Text(84, 17, 136, 8, "DESCRIÇÃO DOS SERVIÇOS PRESTADOS", F(5.25));
        canvas.Text(3, 26, 737, 47, $"  {vm.InfoServicoQuantidade}                         {vm.InfoServicoDescricao}", F(6.75));

        canvas.AdvanceBand(h);
    }

    // ------------------------------------------------------------------
    // rlb_06_ValorPrestacao (dfm line 4472) - componentes / totais / ICMS / tributos federais
    // ------------------------------------------------------------------
    private static void RenderValorPrestacao(ReportCanvas canvas, DacteViewModel vm)
    {
        double h = vm.ModeloOS ? 144 : 117;
        EnsureSpace(canvas, h);
        canvas.Rect(0, 0, 741, h);
        canvas.Text(6, 3, 732, 12, "COMPONENTES DO VALOR DA PRESTAÇÃO DE SERVIÇO", F(6.75), TextAlign.Center);
        canvas.Text(3, 5, 300, 12, "NOME", F(6.75), TextAlign.Left);
        canvas.Text(153, 5, 300, 12, "VALOR", F(6.75), TextAlign.Left);
        canvas.Line(1, 16, 741, 16, 0.5);
        canvas.Line(186, 16, 186, 78, 0.5);
        canvas.Line(372, 16, 372, 78, 0.5);
        canvas.Line(556, 16, 556, 78, 0.5);

        var col1 = vm.ComponentesPrestacao.Where((_, i) => i % 3 == 0).ToList();
        var col2 = vm.ComponentesPrestacao.Where((_, i) => i % 3 == 1).ToList();
        var col3 = vm.ComponentesPrestacao.Where((_, i) => i % 3 == 2).ToList();
        canvas.Memo(5, 19, 96, col1.Select(c => c.Item1), F(6.75));
        canvas.Memo(104, 19, 78, col1.Select(c => c.Item2), F(6.75), TextAlign.Right);
        canvas.Memo(190, 19, 96, col2.Select(c => c.Item1), F(6.75));
        canvas.Memo(290, 19, 78, col2.Select(c => c.Item2), F(6.75), TextAlign.Right);
        canvas.Memo(377, 19, 96, col3.Select(c => c.Item1), F(6.75));
        canvas.Memo(476, 19, 78, col3.Select(c => c.Item2), F(6.75), TextAlign.Right);

        canvas.Text(560, 19, 96, 9, "VALOR TOTAL DO SERVIÇO", F(5.25));
        canvas.Text(570, 29, 164, 14, vm.ValorTotalServico, F(8.25, true), TextAlign.Right);
        canvas.Text(560, 49, 96, 9, "VALOR A RECEBER", F(5.25));
        canvas.Text(570, 61, 164, 14, vm.ValorTotalReceber, F(8.25, true), TextAlign.Right);

        canvas.Line(1, 78, 741, 78, 0.5);
        canvas.Text(8, 79, 728, 13, "INFORMAÇÕES RELATIVAS AO IMPOSTO", F(6.75), TextAlign.Center);
        canvas.Text(3, 95, 81, 8, "SITUAÇÃO TRIBUTÁRIA", F(5.25));
        canvas.Text(350, 95, 66, 8, "BASE DE CÁLCULO", F(5.25));
        canvas.Text(454, 95, 39, 8, "ALÍQ. ICMS", F(5.25));
        canvas.Text(504, 95, 45, 8, "VALOR ICMS", F(5.25));
        if (vm.Icms.MostrarColunaReducaoBc) canvas.Text(590, 95, 59, 8, "% RED.BC.CALC.", F(5.25));
        if (vm.Icms.MostrarColunaIcmsSt) canvas.Text(656, 95, 29, 8, "ICMS ST", F(5.25));

        canvas.Text(3, 102, 340, 13, vm.Icms.SituacaoTributaria, F(6.75));
        canvas.Text(350, 102, 95, 13, vm.Icms.BaseCalculo, F(6.75, true), TextAlign.Center);
        canvas.Text(454, 102, 41, 13, vm.Icms.Aliquota, F(6.75, true), TextAlign.Center);
        canvas.Text(504, 102, 79, 13, vm.Icms.ValorIcms, F(6.75, true), TextAlign.Center);
        if (vm.Icms.MostrarColunaReducaoBc) canvas.Text(590, 102, 57, 13, vm.Icms.PercentualReducaoBc, F(6.75, true), TextAlign.Center);
        if (vm.Icms.MostrarColunaIcmsSt) canvas.Text(656, 102, 81, 13, vm.Icms.IcmsStLegado, F(6.75, true), TextAlign.Center);

        if (vm.ModeloOS && vm.TributosFederais is not null)
        {
            canvas.Line(1, 93, 741, 93, 0.5);
            canvas.Text(57, 95, 50, 8, "VALOR DO PIS", F(5.25));
            canvas.Text(35, 103, 95, 13, vm.TributosFederais.Pis, F(6.75, true), TextAlign.Right);
            canvas.Text(211, 95, 53, 8, "VALOR COFINS", F(5.25));
            canvas.Text(189, 103, 95, 13, vm.TributosFederais.Cofins, F(6.75, true), TextAlign.Right);
            canvas.Text(319, 95, 107, 8, "VALOR DO IMPOSTO DE RENDA", F(5.25));
            canvas.Text(338, 103, 95, 13, vm.TributosFederais.Ir, F(6.75, true), TextAlign.Right);
            canvas.Text(498, 95, 55, 8, "VALOR DO INSS", F(5.25));
            canvas.Text(476, 103, 95, 13, vm.TributosFederais.Inss, F(6.75, true), TextAlign.Right);
            canvas.Text(650, 95, 57, 8, "VALOR DO CSLL", F(5.25));
            canvas.Text(628, 103, 95, 13, vm.TributosFederais.Csll, F(6.75, true), TextAlign.Right);
        }

        canvas.AdvanceBand(h);
    }
}
