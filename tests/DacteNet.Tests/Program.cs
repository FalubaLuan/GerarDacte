using System.Text;
using DacteNet;
using DacteNet.Models;
using DacteNet.Xml;

int failures = 0;
int passed = 0;

void Check(string name, Action action)
{
    try
    {
        action();
        passed++;
        Console.WriteLine($"[OK]   {name}");
    }
    catch (Exception ex)
    {
        failures++;
        Console.WriteLine($"[FAIL] {name}: {ex.Message}");
    }
}

void Assert(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

string FindTestDataXml()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    for (int i = 0; i < 8 && dir is not null; i++)
    {
        var candidate = Path.Combine(dir.FullName, "testdata", "cte_v4_rodoviario_exemplo.xml");
        if (File.Exists(candidate)) return candidate;
        dir = dir.Parent;
    }
    throw new FileNotFoundException("Could not locate testdata/cte_v4_rodoviario_exemplo.xml by walking up from " + AppContext.BaseDirectory);
}

var xmlPath = FindTestDataXml();
var xml = File.ReadAllText(xmlPath, Encoding.UTF8);

Check("Parses the sample CT-e XML without throwing", () =>
{
    var cte = CteXmlParser.Parse(xml);
    Assert(cte.ChaveAcesso.Length == 44, $"expected a 44-digit chave, got '{cte.ChaveAcesso}' ({cte.ChaveAcesso.Length} chars)");
    Assert(cte.ChaveAcesso.All(char.IsDigit), "chave de acesso must be all-digits");
    Assert(cte.Emitente.RazaoSocial == "Transportadora Exemplo Ltda", "unexpected emitente xNome");
    Assert(cte.Identificacao.Modal == Modal.Rodoviario, "expected modal Rodoviario");
    Assert(cte.InfoNormal?.Rodoviario?.Veiculos.Count == 1, "expected exactly one veiculo in rodo");
    Assert(cte.Protocolo?.NumeroProtocolo == "135260000123456", "unexpected protocolo number");
});

Check("Rejects an unsupported document type (GTVe)", () =>
{
    var gtveXml = "<GTVe><infGTV Id=\"GTVe1\" versao=\"4.00\"></infGTV></GTVe>";
    bool threw = false;
    try { CteXmlParser.Parse(gtveXml); }
    catch (CteXmlException) { threw = true; }
    Assert(threw, "expected CteXmlException for a GTVe document");
});

Check("Generates a well-formed A4 PDF from the sample XML", () =>
{
    var pdfBytes = new Dacte().GerarPdfBytes(xml);
    Assert(pdfBytes.Length > 500, "PDF output looks too small");
    var header = Encoding.ASCII.GetString(pdfBytes, 0, 8);
    Assert(header.StartsWith("%PDF-"), $"PDF must start with %PDF- header, got '{header}'");
    var tail = Encoding.ASCII.GetString(pdfBytes, Math.Max(0, pdfBytes.Length - 16), Math.Min(16, pdfBytes.Length));
    Assert(tail.Contains("%%EOF"), "PDF must end with an %%EOF marker");
});

Check("Generates a well-formed A5 PDF from the sample XML", () =>
{
    var options = new DacteOptions { TamanhoPapel = TamanhoPapel.A5 };
    var pdfBytes = new Dacte(options).GerarPdfBytes(xml);
    Assert(pdfBytes.Length > 500, "A5 PDF output looks too small");
    Assert(Encoding.ASCII.GetString(pdfBytes, 0, 8).StartsWith("%PDF-"), "A5 PDF must start with %PDF- header");
});

Check("GerarPdf(xml, path) writes a readable PDF file to disk", () =>
{
    var tempPath = Path.Combine(Path.GetTempPath(), $"dactenet-test-{Guid.NewGuid():N}.pdf");
    try
    {
        new Dacte().GerarPdf(xml, tempPath);
        Assert(File.Exists(tempPath), "expected output PDF file to exist");
        var bytes = File.ReadAllBytes(tempPath);
        Assert(bytes.Length > 500, "output PDF file looks too small");
    }
    finally
    {
        if (File.Exists(tempPath)) File.Delete(tempPath);
    }
});

Check("Homologação environment produces the expected watermark text", () =>
{
    var cte = CteXmlParser.Parse(xml);
    var vm = DacteNet.ViewModel.DacteViewModelBuilder.Build(cte);
    Assert(vm.MensagemStatus == "CT-e SEM VALOR FISCAL - AMBIENTE DE HOMOLOGAÇÃO",
        $"unexpected watermark text: '{vm.MensagemStatus}'");
});

Console.WriteLine();
Console.WriteLine($"{passed} passed, {failures} failed.");
Environment.Exit(failures == 0 ? 0 : 1);
