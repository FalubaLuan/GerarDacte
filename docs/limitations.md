# Known differences and limitations

This is the complete, honest list of every place this port simplifies, infers, or cannot reproduce
something from the original ACBrCTe DACTE renderer. Nothing below was silently dropped - each item is
also called out at the point in the code where it applies (search for "limitations.md" in the source
for the exact spot).

## Out of scope by design (per the original request, not a defect)

- **CT-e emission, digital signature, SEFAZ web-service communication, authorization / consultation /
  cancellation, carta de correção, inutilização, DF-e distribution / events, manifestação.** This
  library only turns an already-issued CT-e's XML into a DACTE PDF.
- **True contingency as a workflow** (generating a contingency key, EPEC transmission, etc.) is not
  implemented - only its *display* aspect on the DACTE is (see "Cannot be reproduced" below).
- **Modelo 64 (GTVe) and CT-e Simplificado** are rejected by `Xml/CteXmlParser` with a
  `CteXmlException` before any DACTE logic runs. They use a different XML schema and, in ACBr, a
  materially different form/layout - reproducing them would mean reverse-engineering a second report,
  not "the DACTE" this request scoped in.
- **Non-DACTE documents** (DANFE, etc.) and every other ACBr component - untouched, not analyzed.

## Cannot be reproduced from the analyzed source

- **True-contingency / EPEC barcode contents.** When a CT-e is in `teContingencia`/`teFSDA` (not yet
  authorized) or `teDPEC`, ACBr draws a barcode of `fpACBrCTe.GerarChaveContingencia(fpCTe)` - a
  function that lives entirely outside the two DACTE report files that were the scope of this port (it
  belongs to the emission/webservices side of ACBrCTe). `DacteViewModel.UsarBarcodeContingencia` is
  still set so a caller can detect the situation, but `BarcodeDigitos`/`TextoProtocolo` are left blank
  rather than inventing a key. A production integrator who needs this must compute the contingency key
  themselves (via whatever emission library they already use) and pass it in - there is currently no
  option slot for that; it would be a reasonable future addition to `DacteOptions`.

## Deliberate scope/inference decisions

- **"Documentos originários" CNPJ/CPF column.** ACBr's FastReport-based DACTE variant
  (`CarregaDadosNotasFiscais`) explicitly shows the *Remetente's* CNPJ/CPF on every NF/NF-e/Outros row.
  The Report-Lite variant analyzed for this port (`Itens` procedure) is only described narratively in
  the layout analysis, not quoted verbatim - so this port uses the same Remetente-CNPJ convention for
  consistency across ACBr's own two rendering engines. Flagged as an inference, not a verified fact,
  in `DacteViewModelBuilder.BuildDocumentosOriginarios`.
- **Legacy (&lt;3.00) "forma de pagamento"** (`ide/forPag`) is not modeled - `Models.Identificacao` has
  no such field. This is an intentionally out-of-scope field: it was removed from the CT-e schema in
  version 3.00 and every realistic input to this library will be 3.00+. The header
  "FORMA DE PAGAMENTO" slot is therefore only ever populated with the v≥3.00 "CT-e Globalizado"
  observation text, never a legacy forma-de-pagamento string.
- **CST description text** (`CSTICMSToStrTagPosText`) is standard SEFAZ CST-table wording, written from
  general domain knowledge of the CT-e schema, not transcribed from an ACBr source file - the function
  that generates it lives outside the two analyzed DACTE files.
- **The canhoto summary boilerplate sentence** and the fixed rodoviário-legislation notice on the A5
  layout ("ESSE CT-e DE TRANSP. ATENDE LEGISLAÇÃO DE TRANSP. RODO.EM VIGOR") are transcribed verbatim
  from the source, but a couple of adjacent short fixed strings elsewhere (e.g. the canhoto's
  "Recebemos de..." style boilerplate, where present) were reconstructed as standard DACTE wording
  rather than pulled from a file that quoted them character-for-character.
- **`infQ`/`cUnid` unit-of-measure table.** `TUnidMed` (the type behind `cUnid`) is declared in a shared
  ACBr unit that is not part of the ACBrCTe-only source tree provided, so its string↔code mapping could
  not be read directly from source. `Xml/CteEnumParsers.UnidadeMedidaCarga` uses the mapping
  `00=M3, 01=KG, 02=TON, 03=UNIDADE, 04=LITROS, 05=MMBTU`, cross-checked against this library's own test
  fixture rather than against ACBr source - treat this table as **higher-confidence but not
  independently source-verified**; an integrator working against a real SEFAZ-authorized CT-e should
  spot-check a known document's peso/cubagem values against its rendered DACTE at least once.
- **"FOLHA" (page X of Y).** Both the A4 and A5 originals print a `current/total` page token
  (`RLSystemInfo1`/`rllPageNumber`) next to a static "FOLHA" label. This port prints the static label
  but leaves the value blank - the renderer draws one page at a time and does not know the total page
  count in advance the way a native Fortes Report component (which lays out the whole document before
  printing) does. Computing it would mean a two-pass render (once to count pages, once to draw); not
  implemented.
- **Issuer logo (`DacteOptions.Logo`) and custom watermark image (`DacteOptions.MarcaDeAgua`).** Both
  option slots exist (mirroring ACBr's `fpDACTe.Logo`/`fpDACTe.MarcaDagua` and `ExpandeLogoMarca`), and
  `PdfImage` can embed a JPEG, but neither renderer currently *draws* them (`rliLogo`/`rliMarcadAgua`
  are not yet wired up in `Rendering/A4`/`Rendering/A5`). Only JPEG would be supported when this is
  implemented - see the next item.

- **"Documentos originários" CNPJ/CPF + document-number column.** The original report keeps these as
  two separately-aligned sub-columns (`CNPJ/CPF EMITENTE` widened to fill the row when there is no
  paired document number, `SÉRIE/NRO. DOCUMENTO` otherwise). `DacteViewModel.LinhasDocumentosOriginarios`
  stores them pre-combined as a single `"{cnpj} {documento}"` string per row instead, printed once under
  both header labels. Visually this reads the same for the common case (both values present); it would
  only look different from the original in the edge case of a document type with no separate document
  number at all, where ACBr would left-align the CNPJ across the widened combined space and this port
  instead prints "cnpj " with a trailing space where the number would have gone.

## Known simplifications in the rendering

- **Purely decorative grid/divider lines** (`TRLDraw` controls with no runtime logic behind them) are
  reproduced for every primary border and column divider, but not exhaustively for every minor hairline
  in the densest tabular bands (vale-pedágio, the A5 compact card). Data placement itself is always
  faithful; only some non-load-bearing ruling lines are pragmatically omitted.
- **The alternate "barra" (strip) canhoto layout** (`DacteOptions.LayoutCanhoto = LayoutCanhoto.Barra`)
  is accepted as an option but not implemented separately - the standard canhoto layout is used
  regardless of this setting.
- **A5 modal-specific blocks.** The A5 "retrato simplificado" layout omits the rodoviário
  "lotação"/vale-pedágio table, and the aéreo/aquaviário specialty blocks that exist on A4
  (`rlb_11_ModRodLot104`, `rlb_12_ModAereo`, `rlb_13_ModAquaviario` in ACBr's own A5 report). A4 is the
  size ACBr itself recommends for those modals; this port follows that same recommendation rather than
  re-deriving three more compact-layout bands for a paper size that is not meant to carry them.
- **Ferroviário/Dutoviário have no modal-specific section in either paper size** - this is not a gap in
  this port, it is a property of the source layout itself (ACBr's own matching case branches force
  those bands to `Height:=0` and, on A5, the bands have no child controls at all).
- **`infUnidTransp`/`infUnidCarga` container/lacre nesting detail** (container numbers, seal numbers
  inside a transport unit, `qtdRat` ratios) is parsed only as far as the DACTE needs it and is not
  rendered as its own block - the original DACTE layout does not have a dedicated section for this
  either; it is schema detail that exists for other consumers of the XML, not for this printed document.
- **A5's long-form status watermark** (`vm.MensagemStatus`, e.g. "CT-e SEM VALOR FISCAL - AMBIENTE DE
  HOMOLOGAÇÃO") is drawn at the original's fixed 27-raw-unit (20.25pt) bold font regardless of string
  length; on the narrower A5 page a long status string can run a few points past the right margin
  (confirmed by measuring the rendered PDF: about 4.5pt over a ~595pt-wide page for the homologação
  text used in this project's own test fixture). The original Delphi label has the same fixed-font,
  no-autosize behavior, so this is a faithfully-reproduced tight fit rather than a new defect - but it
  is the one place in the whole layout where a real (if very short) production string could be visibly
  clipped by a printer's edge.
- **`ReportCanvas.Text()` vertical anchoring** places the text baseline a fixed offset (`0.8em`) below
  the top of its box, approximating the original Delphi `TRLLabel`/`TRLMemo` controls (whose boxes are
  typically only slightly taller than one text line, so their content sits at the top, not centered).
  This is an approximation of "how a label looks", not a transcription of Delphi's own text-metrics
  code, and could show very slightly different vertical spacing than the original on an
  unusually-tall single-line box.
- **PDF content streams are written uncompressed.** This makes generated files somewhat larger than a
  typical PDF writer's output and easier to inspect/diff by hand; it has no effect on how the document
  looks or prints.
- **Only JPEG is supported for any embedded raster image** (`Rendering/Primitives/PdfImage`) - relevant
  once the logo/watermark image slots above are wired up; a PNG logo would need to be converted to JPEG
  before being passed in.
- **Standard-14 font metrics (AFM widths)** are hand-transcribed for the Latin/Portuguese character set
  this document actually uses (including the accented letters in Portuguese words); a handful of very
  rarely-used symbols outside that set fall back to an approximate average width rather than an
  AFM-exact one.
- **QR Code encoding** supports the version/error-correction tables needed for the URL lengths a real
  `infCTeSupl/qrCodCTe` value produces in practice; the version-25+ end of the built-in table is
  lower-confidence than the common low-version range, since it was less exercised during testing.

## Test data

No sample/test CT-e XML exists anywhere in the provided ACBrCTe source tree (confirmed by searching the
whole tree) - `testdata/cte_v4_rodoviario_exemplo.xml` is therefore a synthetic, fictitious CT-e v4.00
document, authored from the CT-e 4.00 schema and this project's own XML-mapping analysis, used as the
fixture for both the automated tests and the sample app. All parties, keys, and values in it are made
up and must not be treated as a real transport document.
