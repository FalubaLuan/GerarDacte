namespace DacteNet.Models;

public sealed class ComponenteValorPrestacao
{
    public string? Nome { get; set; }     // xNome
    public decimal Valor { get; set; }     // vComp
}

/// <summary>infCTe/vPrest - valor da prestação do serviço.</summary>
public sealed class ValorPrestacao
{
    public decimal ValorTotalPrestacao { get; set; }   // vTPrest
    public decimal ValorReceber { get; set; }            // vRec
    public List<ComponenteValorPrestacao> Componentes { get; set; } = new();
}
