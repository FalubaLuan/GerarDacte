// Minimal usage example for the DacteNet library.
//
// Run from the repository root with:
//   dotnet run --project samples/DacteNet.Sample/DacteNet.Sample.csproj -- <path-to-cte-xml> [output.pdf]
//
// If no XML path is given, it falls back to the library's own test fixture
// (testdata/cte_v4_rodoviario_exemplo.xml), so `dotnet run` works out of the box with no arguments.

using DacteNet;
using DacteNet.Models;

string xmlPath = args.Length > 0 ? args[0] : FindDefaultSampleXml();
string outputPath = args.Length > 1 ? args[1] : "dacte.pdf";

string xml = File.ReadAllText(xmlPath);

// 1) Simplest possible call: parse the CT-e XML and write an A4 DACTE PDF next to it.
new Dacte().GerarPdf(xml, outputPath);
Console.WriteLine($"A4 DACTE written to {Path.GetFullPath(outputPath)}");

// 2) The same document, rendered as the compact A5 "retrato simplificado" layout instead, with a
//    couple of print-time options set (none of this comes from the XML - see DacteOptions).
var a5Options = new DacteOptions
{
    TamanhoPapel = TamanhoPapel.A5,
    Sistema = "DacteNet Sample",
    Usuario = Environment.UserName,
    
};
string a5OutputPath = Path.ChangeExtension(outputPath, null) + "_a5.pdf";
new Dacte(a5Options).GerarPdf(xml, a5OutputPath);
Console.WriteLine($"A5 DACTE written to {Path.GetFullPath(a5OutputPath)}");

// 3) GerarPdfBytes(...) is available too, for callers that want the raw bytes (e.g. to stream a
//    download response) instead of writing straight to a file path.
byte[] pdfBytes = new Dacte().GerarPdfBytes(xml);
Console.WriteLine($"GerarPdfBytes(...) returned {pdfBytes.Length} bytes.");

static string FindDefaultSampleXml()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    for (int i = 0; i < 8 && dir is not null; i++)
    {
        var candidate = Path.Combine(dir.FullName, "testdata", "42260803007331021653570010309526701861261412.xml");
        if (File.Exists(candidate)) return candidate;
        dir = dir.Parent;
    }
    throw new FileNotFoundException(
        "No XML path was given and the bundled sample (testdata/cte_v4_rodoviario_exemplo.xml) could not be located. " +
        "Pass a CT-e XML file path as the first argument.");
}
