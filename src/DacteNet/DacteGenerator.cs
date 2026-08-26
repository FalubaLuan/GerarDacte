using DacteNet.Models;
using DacteNet.Rendering.A4;
using DacteNet.Rendering.A5;
using DacteNet.ViewModel;
using DacteNet.Xml;

namespace DacteNet;

/// <summary>
/// Public entry point of the library: turns a CT-e XML document into a DACTE PDF.
///
/// Pipeline: <see cref="CteXmlParser"/> (XML -&gt; <see cref="Models.CteDocument"/>) -&gt;
/// <see cref="DacteViewModelBuilder"/> (business rules -&gt; <see cref="DacteViewModel"/>) -&gt;
/// <see cref="DacteA4Renderer"/> or <see cref="DacteA5Renderer"/> (layout -&gt; PDF bytes), all built
/// from scratch with zero third-party NuGet dependencies (see docs/README.md "Why a hand-written PDF
/// engine" for the rationale).
///
/// Usage:
/// <code>
/// var pdfBytes = new Dacte().GerarPdfBytes(xml);
/// new Dacte().GerarPdf(xml, "dacte.pdf");
/// </code>
/// </summary>
public sealed class Dacte
{
    private readonly DacteOptions _options;

    public Dacte(DacteOptions? options = null) => _options = options ?? new DacteOptions();

    /// <summary>Parses the CT-e XML and renders the DACTE PDF, returning the raw PDF bytes.</summary>
    public byte[] GerarPdfBytes(string cteXml)
    {
        var cte = CteXmlParser.Parse(cteXml);
        var vm = DacteViewModelBuilder.Build(cte, _options);
        return _options.TamanhoPapel == TamanhoPapel.A5
            ? new DacteA5Renderer().Render(vm).ToBytes()
            : new DacteA4Renderer().Render(vm).ToBytes();
    }

    /// <summary>Parses the CT-e XML and writes the rendered DACTE PDF to <paramref name="path"/>.</summary>
    public void GerarPdf(string cteXml, string path) => File.WriteAllBytes(path, GerarPdfBytes(cteXml));

    /// <summary>Parses the CT-e XML and writes the rendered DACTE PDF to <paramref name="stream"/>.</summary>
    public void GerarPdf(string cteXml, Stream stream)
    {
        var bytes = GerarPdfBytes(cteXml);
        stream.Write(bytes, 0, bytes.Length);
    }
}
