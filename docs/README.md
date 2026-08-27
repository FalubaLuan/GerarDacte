# DacteNet

DacteNet é uma biblioteca .NET para conversão de XMLs de Conhecimento de Transporte Eletrônico (CT-e) em documentos PDF do DACTE (Documento Auxiliar do Conhecimento de Transporte Eletrônico).

## Recursos Suportados

* **Modelos aceitos:** 57 (CT-e) e 67 (CT-e OS).
* **Estrutura XML:** Aceita elementos `<CTe>` isolados ou envelopes `<cteProc>` completos.
* **Versões do layout:** 2.00, 3.00 e 4.00.
* **Formatos de página:** A4 (padrão) e A5 (retrato simplificado).
* **Recursos visuais:** Código de barras Code128, QR Code e marca d'água de homologação.
* **Escopo:** Exclusivamente geração do PDF do DACTE. Não realiza assinatura digital, envio para SEFAZ, consulta ou cancelamento.

---

## Instalação e Uso Básico

### 1. Carregamento do XML e Geração do PDF

A classe `Dacte` é o ponto de entrada da biblioteca. O XML pode ser passado como `string` para gravação direta em disco, conversão para `byte[]` ou gravação em `Stream`.

```csharp
using System.IO;
using DacteNet;

string xmlContent = File.ReadAllText("cte.xml");

// Salvar diretamente em um arquivo
new Dacte().GerarPdf(xmlContent, "dacte.pdf");

// Obter os bytes do PDF (ex: para retorno em APIs)
byte[] pdfBytes = new Dacte().GerarPdfBytes(xmlContent);

// Gravar em uma Stream existente
using var stream = File.Create("dacte.pdf");
new Dacte().GerarPdf(xmlContent, stream);
```

---

## Configurações de Impressão (DacteOptions)

Atributos de cabeçalho, rodapé e formato de página que não pertencem ao XML do CT-e devem ser definidos no objeto `DacteOptions` e passados ao construtor.

```csharp
using DacteNet;

var options = new DacteOptions
{
    // Formato do papel: TamanhoPapel.A4 (padrão) ou TamanhoPapel.A5
    TamanhoPapel = TamanhoPapel.A4,

    // Dados exibidos no bloco do emitente
    Site = "www.suaempresa.com.br",
    Email = "atendimento@suaempresa.com.br",

    // Dados de identificação exibidos no rodapé
    Sistema = "Nome do Sistema / ERP",
    Usuario = "operador",

    // Exibe a linha de resumo do canhoto (Apenas layout A4)
    ExibirResumoCanhoto = true,

    // Define a posição do canhoto (Apenas layout A4)
    // Opções: PosicaoCanhoto.Topo (padrão) ou PosicaoCanhoto.RodaPe
    PosicaoCanhoto = PosicaoCanhoto.Topo,

    // Força a exibição da tarja "CT-E CANCELADO"
    Cancelar = false,

    // Permite informar um protocolo manualmente caso o XML não possua o <protCTe>
    Protocolo = "135240000000000 - 01/01/2026 10:00:00"
};

new Dacte(options).GerarPdf(xmlContent, "dacte_customizado.pdf");
```

---

## Propriedades da Classe DacteOptions

| Propriedade | Tipo | Valor Padrão | Descrição |
| :--- | :--- | :--- | :--- |
| `TamanhoPapel` | `TamanhoPapel` | `A4` | Define o layout e dimensão do papel (`A4` ou `A5`). |
| `PosicaoCanhoto` | `PosicaoCanhoto` | `Topo` | Posição do canhoto no papel (`Topo` ou `RodaPe`). |
| `ExibirResumoCanhoto` | `bool` | `false` | Exibe a linha resumida no canhoto (Layout A4). |
| `Sistema` | `string` | `""` | Identificação do software impressa no rodapé. |
| `Usuario` | `string` | `""` | Nome do usuário logado impresso no rodapé. |
| `Site` | `string` | `""` | URL da empresa exibida no bloco de endereço do emitente. |
| `Email` | `string` | `""` | E-mail exibido no bloco de endereço do emitente. |
| `Cancelar` | `bool` | `false` | Força a renderização da marca d'água de documento cancelado. |
| `Protocolo` | `string` | `""` | Override do número e data do protocolo de autorização. |

---

## Tratamento de Exceções

Erros de parseamento ou validação do XML do CT-e lançam a exceção `CteXmlException`.

```csharp
using DacteNet;
using DacteNet.Xml;

try
{
    string xmlInvalido = "<CTe><infCte mod="64"></infCte></CTe>"; // Modelo 64 não suportado
    new Dacte().GerarPdf(xmlInvalido, "saida.pdf");
}
catch (CteXmlException ex)
{
    // Lançado para modelos não aceitos (GTVe / CT-e Simplificado) ou XML malformado
    System.Console.WriteLine($"Erro ao processar XML do CT-e: {ex.Message}");
}
```