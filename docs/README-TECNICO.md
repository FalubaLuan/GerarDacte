# DacteNet

DacteNet is an independent .NET class library that generates a **DACTE** (Documento Auxiliar do
Conhecimento de Transporte Eletrônico) PDF from a CT-e XML document. It is modeled on the layout and
business rules of the DACTE-rendering portion of the [ACBr](https://www.projetoacbr.com.br/) project's
`ACBrCTe` Delphi component, reimplemented from scratch in C#.

It does **not** implement CT-e emission, digital signature, SEFAZ web-service communication,
authorization/consultation/cancellation, carta de correção, inutilização, DF-e distribution/events,
manifestação, or any other ACBr component. Given a CT-e XML, it does one thing: it interprets the XML
and renders the DACTE PDF that accompanies the cargo in transit.

## Quick start

```csharp
using DacteNet;

string xml = File.ReadAllText("meu-cte.xml");

// Simplest call: parse the XML and write an A4 DACTE PDF.
new Dacte().GerarPdf(xml, "dacte.pdf");

// Or get the raw bytes (e.g. to stream a download response):
byte[] pdfBytes = new Dacte().GerarPdfBytes(xml);

// Or write to an already-open stream:
using var stream = File.Create("dacte.pdf");
new Dacte().GerarPdf(xml, stream);
```

The XML can be a bare `<CTe>` document or the full `<cteProc>` envelope (`<CTe>` + `<protCTe>`), with or
without the `http://www.portalfiscal.inf.br/cte` namespace declared, and in any of the CT-e versions
ACBr itself supports (2.00/3.00/4.00). Only modelo 57 (CT-e) and modelo 67 (CT-e OS) documents are
accepted; a modelo 64 (GTVe) or CT-e Simplificado document throws `CteXmlException` - see
[Limitations](limitations.md).

### Print-time options

Some things that appear on a DACTE are not part of the CT-e XML at all - they are operator/software
preferences, the same way ACBr keeps them on the DACTE *component* rather than on the document. These
go on `DacteOptions`, passed to the `Dacte` constructor:

```csharp
var options = new DacteOptions
{
    TamanhoPapel = TamanhoPapel.A5,      // A4 (default) or A5 "retrato simplificado"
    Sistema = "Meu ERP",                  // printed in the footer strip
    Usuario = "fulano",
    Site = "www.minhatransportadora.com.br",   // extra line in the issuer address block
    Email = "contato@minhatransportadora.com.br",
    ExibirResumoCanhoto = true,            // A4 only - adds the one-line canhoto summary
};

new Dacte(options).GerarPdf(xml, "dacte_a5.pdf");
```

See `DacteOptions.cs` for the full list (canhoto position/layout, forced "cancelada" banner, a manual
protocolo override, printed hora-de-saída, and issuer logo/watermark image slots - the last two are
accepted by the options object but not yet rendered, see [Limitations](limitations.md)).

## Project layout

```
src/DacteNet/
  DacteGenerator.cs        Dacte - the public entry point (GerarPdf/GerarPdfBytes)
  DacteOptions.cs           print-time configuration, not read from the XML
  Xml/                      CT-e XML -> internal Models.CteDocument (no XSD-generated classes)
  Models/                   plain C# classes: the internal, DACTE-relevant slice of a CT-e
  ViewModel/                Models.CteDocument -> DacteViewModel (all business rules/calculations live here)
  Rendering/
    Primitives/             hand-written PDF writer, ReportCanvas, standard-14 font metrics, WinAnsi encoding
    Barcode/, Qr/            Code128 and QR Code, drawn as vector shapes (no image encoder involved)
    A4/, A5/                 band-by-band layout, one file per renderer, reading only from DacteViewModel
tests/DacteNet.Tests/        plain console "assert or throw" test runner (see below)
samples/DacteNet.Sample/     minimal usage example
testdata/                    a synthetic (fictitious) CT-e v4.00 XML used by the tests and sample
docs/                        this file, limitations.md, acbr_reference_mapping.md
```

The pipeline is deliberately linear and each stage only talks to its neighbors:

```
CT-e XML --[Xml/CteXmlParser]--> Models.CteDocument --[ViewModel/DacteViewModelBuilder]--> DacteViewModel --[Rendering/A4 or A5]--> PDF bytes
```

`Models.CteDocument` is a plain, hand-shaped set of classes covering only the elements the DACTE
actually uses - not a generated 1:1 mirror of the CT-e XSD. `DacteViewModel` is the print-ready,
already-formatted, already-decided view: renderers never touch `CteDocument` or make a business
decision themselves, they only place already-computed strings/numbers at fixed coordinates.

## Why a hand-written PDF engine instead of a NuGet library

This was a deliberate, and not entirely voluntary, choice - worth stating plainly since it shapes a lot
of the code in `Rendering/Primitives`:

1. **The sandbox this library was built in has no access to nuget.org** (`nuget.config` here clears all
   package sources). Any PDF library dependency - iText, PdfSharp, QuestPDF, SkiaSharp for
   barcodes/QR, ZXing.Net, whatever - would have made the project simply not buildable in that
   environment, let alone testable end-to-end.
2. Even setting the sandbox aside, a DACTE needs a fairly specific, low-level feature set:
   **absolute-positioned text, lines, filled rectangles, a Code128 barcode, a QR Code, multi-page flow,
   and standard fonts with predictable metrics** - it does not need rich text layout, HTML/CSS
   rendering, form fields, or any of the heavier machinery most general-purpose PDF libraries carry.
   A ~1500-line hand-written writer covers exactly this surface, with **zero runtime dependencies** and
   no licensing questions (some of the well-known .NET PDF libraries are commercial/AGPL for anything
   beyond a trivial document).
3. It keeps the fidelity story auditable: `ReportCanvas` draws in the *same raw design units* ACBr's own
   layout uses (1 raw unit = 1/96in = 0.75pt, see `Rendering/Primitives/Geometry.cs`), so every drawing
   call in `Rendering/A4`/`Rendering/A5` can be checked directly against the extracted
   `analysis/retrato_layout.md` / `analysis/retrato_a5_layout.md` component tables, coordinate by
   coordinate - a third-party layout engine with its own coordinate/flow model would have obscured that
   traceability.

The trade-off, in the interest of not overselling this: the PDF writer only supports the 14 standard
PDF fonts (Times-Roman/Bold used throughout, to match "Times New Roman" in the original), content
streams are written uncompressed, and JPEG is the only supported embedded raster image format. None of
that limits anything a DACTE itself needs - see [Limitations](limitations.md) for the complete,
honest list of gaps.

## Tests

There is no xUnit/NUnit/MSTest dependency, for the same reason as above (no NuGet access, and the
library's own zero-dependency policy). `tests/DacteNet.Tests` is a plain console `Exe` project with a
small `Check`/`Assert` helper that prints `[OK]`/`[FAIL]` per case and exits non-zero on any failure:

```
dotnet run --project tests/DacteNet.Tests/DacteNet.Tests.csproj
```

It parses the bundled test XML, generates both an A4 and an A5 PDF from it and checks they are
well-formed (`%PDF-` header, `%%EOF` trailer, non-trivial size), writes a PDF to a file path and checks
it round-trips, and checks the homologação watermark text. This is a smoke/regression test, not a
substitute for opening the PDF - every layout fix in this codebase was actually found and verified by
rendering a PDF to PNG (`pdftoppm`) and looking at it, not by the automated tests alone; see
`docs/acbr_reference_mapping.md` for how the layout coordinates were sourced and cross-checked.

## Which ACBrCTe files this was built from

See [docs/acbr_reference_mapping.md](acbr_reference_mapping.md).

## Known differences and limitations

See [docs/limitations.md](limitations.md).
