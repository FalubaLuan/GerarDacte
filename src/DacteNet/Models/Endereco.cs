namespace DacteNet.Models;

/// <summary>
/// Generic address block. In the ACBr Pascal model this shape is repeated, with minor variations,
/// as TEndereco / TEnderEmit / TEnderFerro / TLocColeta / TLocEnt (see cte_model.md §2 note 4).
/// Unified here into a single type with nullable extras.
/// </summary>
public sealed class Endereco
{
    public string? Logradouro { get; set; }      // xLgr
    public string? Numero { get; set; }           // nro
    public string? Complemento { get; set; }      // xCpl
    public string? Bairro { get; set; }            // xBairro
    public int? CodigoMunicipio { get; set; }      // cMun
    public string? Municipio { get; set; }         // xMun
    public int? Cep { get; set; }                  // CEP
    public string? Uf { get; set; }                // UF
    public int? CodigoPais { get; set; }            // cPais
    public string? Pais { get; set; }               // xPais
    public string? Telefone { get; set; }           // fone (present on emitter/party addresses, not on all shapes)
}

/// <summary>Lightweight pickup/delivery location (locColeta / locEnt) - carries its own party identity.</summary>
public sealed class LocalColetaEntrega
{
    public string? CnpjCpf { get; set; }
    public string? Nome { get; set; }
    public string? Logradouro { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public int? CodigoMunicipio { get; set; }
    public string? Municipio { get; set; }
    public string? Uf { get; set; }
}
