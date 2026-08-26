namespace DacteNet.Models;

public sealed class Emitente
{
    public string? Cnpj { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? InscricaoEstadualST { get; set; }
    public string? RazaoSocial { get; set; }        // xNome
    public string? NomeFantasia { get; set; }        // xFant
    public Endereco Endereco { get; set; } = new();
    public RegimeTributario? Crt { get; set; }
}

/// <summary>Tomador do serviço, quando lido do grupo dedicado &lt;toma&gt; (versão &gt;= 3.00 / CT-e OS / CT-e Simplificado).</summary>
public sealed class TomadorServico
{
    public string? CnpjCpf { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? RazaoSocial { get; set; }
    public string? NomeFantasia { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string? InscricaoSuframa { get; set; }
    public Endereco Endereco { get; set; } = new();
}

public sealed class Remetente
{
    public string? CnpjCpf { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? RazaoSocial { get; set; }
    public string? NomeFantasia { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public Endereco Endereco { get; set; } = new();
    public LocalColetaEntrega? LocalColeta { get; set; }
}

public sealed class Expedidor
{
    public string? CnpjCpf { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? RazaoSocial { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public Endereco Endereco { get; set; } = new();
}

public sealed class Recebedor
{
    public string? CnpjCpf { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? RazaoSocial { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public Endereco Endereco { get; set; } = new();
}

public sealed class Destinatario
{
    public string? CnpjCpf { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? InscricaoSuframa { get; set; }
    public string? RazaoSocial { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public Endereco Endereco { get; set; } = new();
    public LocalColetaEntrega? LocalEntrega { get; set; }
}
