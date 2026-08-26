# ACBrCTe files used as reference

This library was built by reading the ACBrCTe (ACBr Project) Delphi source, extracting the
DACTE-relevant logic into a set of analysis documents, and then reimplementing only that slice in C#.
No ACBrCTe source file was copied or translated line-by-line into this repository; every `.cs` file
here is an original implementation written from the extracted business rules and layout coordinates.

## Files read and used as reference

| ACBrCTe source file | What was taken from it |
|---|---|
| `ACBrCTe/DACTE/Fortes/ACBrCTeDACTeRL.pas` (`TACBrCTeDACTeRL`/`TfrmDACTeRL` base form, read in full) | The shared `Imprimir`/`SalvarPDF` entry points; `GetTextoResumoCanhoto` (quoted verbatim, see `ViewModel/DacteViewModelBuilder.BuildTextoResumoCanhoto`); the general "every band is a header band, one dataset-driven exception" structure that shaped `Rendering/A4/DacteA4Renderer.Render`'s fixed band order. |
| `ACBrCTe/DACTE/Fortes/ACBrCTeDACTeRLRetrato.pas` / `.dfm` (A4 "retrato" report, `TfrmDACTeRLRetrato`) | Every A4 band's `BeforePrint` logic (field mappings, conditional visibility, calculations) and every control's exact `Left,Top,Width,Height` in raw design units - the direct source for `Rendering/A4/*` and the "TIPO DO CT-E"/"TOMADOR DO SERVIÇO" header-toggle logic in `ViewModel/DacteViewModelBuilder.BuildTomadorHeaderIndicator`. |
| `ACBrCTe/DACTE/Fortes/ACBrCTeDACTeRLRetratoA5.pas` / `.dfm` (A5 "retrato simplificado", `TfrmDACTeRLRetratoA5`) | Same, for the A5 layout (`Rendering/A5/DacteA5Renderer`) - including confirming that A5's tomador-do-serviço field (`rllTomaServico`) has no version-dependent "SIM/NÃO CT-e Globalizado" branch, unlike A4's. |
| `ACBrCTe/Base/ACBrCTe.Conversao.pas` (and its duplicate, `ACBrCTe/PCNCTe/pcteConversaoCTe.pas`) | The exact display-text tables for `tpCTToStrText` (TIPO DO CT-E) and `TpServToStrText` (TIPO DO SERVIÇO), reproduced in `ViewModel/DacteViewModelBuilder.TipoCteToStrText`/`TipoServicoToStrText`. |
| `ACBrCTe/Base/ACBrCTe.Classes.pas` | Confirmed `TInfQCollectionItem.cUnid` is typed `TUnidMed`, an externally-defined type not present in this source tree - documented as an inference point in `docs/limitations.md` rather than silently assumed. |
| `ACBrCTe/Base/ACBrCTe.XmlWriter.pas`, `ACBrCTe.IniWriter.pas`, `ACBrCTe.IniReader.pas` | Confirmed how `cUnid`/`UnidMedToStr`/`StrToUnidMed` are used at the call site (read/write symmetry), used to cross-check `Xml/CteEnumParsers.UnidadeMedidaCarga`'s code table. |

## Analysis documents (produced from the above, and used as this library's primary working reference)

The Delphi source above was distilled, before any C# was written, into a set of markdown analysis
documents - these were the documents actually consulted while writing `Rendering/A4`, `Rendering/A5`,
and `ViewModel/DacteViewModelBuilder`, since they organize the same information band-by-band with the
Pascal source quoted inline:

- `analysis/retrato_layout.md` - full component-by-component map of the A4 report, with raw coordinates,
  fonts, and the Pascal logic behind every data-bound field.
- `analysis/retrato_a5_layout.md` - the same for the A5 report.
- `analysis/fastreport_crosscheck.md` - an independent cross-check against ACBr's other (FastReport-based)
  DACTE rendering engine, used especially to validate the Tomador-resolution and canhoto-summary rules
  where the two engines agree, and to flag the one place they might not (documentos originários'
  CNPJ/CPF column - see `docs/limitations.md`).
- `analysis/cte_model.md` - the CT-e object model as ACBr represents it internally, used to decide which
  XML elements the DACTE actually touches (and therefore which ones `Models/*.cs` needed to represent).
- `analysis/xml_mapping.md` - field-by-field XML→Pascal-property mapping, used to write `Xml/CteXmlParser`
  without needing XSD-generated classes.

## What was deliberately *not* used

Everything else in the ACBrCTe source tree - emission (`ACBrCTe.pas`, `ACBrCTeConhecimentos.pas`),
web services (`ACBrCTeWebServices.pas`, `ACBrCTeServicos.*`), configuration
(`ACBrCTeConfiguracoes.pas`), and the registration/component-palette glue (`ACBrCTeReg.pas`) - was
read only far enough to confirm it has no DACTE-rendering logic, then set aside. None of it was
translated or referenced beyond that scoping check. `ACBrCTe/DACTE_extra/` (an alternate/older copy of
the same two report files) was likewise checked only to confirm it was a duplicate, not read as a
separate source of truth.
