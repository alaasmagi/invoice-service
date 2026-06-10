## Context

The application currently supports manual invoice creation for one `Service` at one `Address`, then uses that invoice data for allocations and person monthly statements. Provider invoices from Telia and Enefit may contain multiple pages and multiple addresses, with different visual/text layouts per provider. A single uploaded provider PDF must therefore be classified first, then parsed by a provider-specific parser into address-level invoice candidates that can be matched to existing user-owned services and addresses.

The import feature must preserve the current solution structure: parsing and import orchestration belong in Application/Contracts, persistence remains behind repositories, and Web provides the MVC upload/result workflow. The current `Invoice` model is sufficient for the first implementation because each imported row becomes one address-specific invoice using existing `ServiceId`, `AddressId`, `InvoiceDate`, `PeriodStart`, `PeriodEnd`, and `TotalSum` fields.

## Goals / Non-Goals

**Goals:**

- Upload a Telia or Enefit invoice PDF from the authenticated user's browser session.
- Extract text from all PDF pages.
- Detect the provider/service before running provider-specific parsing.
- Parse Telia and Enefit PDFs with separate parser implementations.
- Match the detected provider to an existing user-owned `Service` record.
- Match parsed address names to existing user-owned `Address` records.
- Create one `Invoice` per matched address amount using the existing invoice creation flow.
- Return an import result showing created invoices, unmatched addresses, skipped rows, parser warnings, and fatal errors.
- Keep all matching and creation scoped to the authenticated user's records.

**Non-Goals:**

- Creating services or addresses automatically from PDF text.
- Persisting the uploaded PDF file itself.
- Supporting providers other than Telia and Enefit in this change.
- Perfect fuzzy matching for every spelling variation; first implementation uses deterministic normalized matching with clear unmatched-address reporting.
- Replacing manual invoice creation.
- Changing monthly statement generation rules.

## Decisions

- Use a PDF text extraction abstraction instead of parsing in the controller.
  - Rationale: Controllers should handle upload and presentation, while Application services own import orchestration. This also makes parser tests possible using extracted text fixtures.
  - Alternative considered: Parse directly in MVC action. Rejected because provider logic would become hard to test and maintain.

- Add provider-specific parser classes behind a common interface.
  - Rationale: Telia and Enefit formats differ, and separate parsers keep fragile layout assumptions isolated.
  - Alternative considered: One parser with provider conditionals. Rejected because multi-page/multi-address invoices will grow provider-specific rules quickly.

- Convert parsed provider rows into existing `Invoice` records rather than adding a new import table first.
  - Rationale: The current invoice model already represents one service/address/date/period/amount. Imported rows should behave exactly like manually created invoices for allocation and monthly statements.
  - Alternative considered: Add `ImportedInvoiceBatch` and row entities immediately. Deferred because the first requirement is importing invoices, not auditing stored PDF batches. The service result can still show batch-level feedback in the UI.

- Match service by normalized provider/service name against existing `Service.Name`.
  - Rationale: The user already maintains service records. Import should not silently create a service that monthly statement rules may not recognize.
  - Alternative considered: Hardcode service IDs or create missing services. Rejected because it would break user ownership and hide configuration mistakes.

- Match addresses by normalized exact match against `Address.Name` and `Address.FullAddress`, with unmatched entries reported.
  - Rationale: Deterministic matching avoids attaching invoice amounts to the wrong household address. Unmatched rows should be visible and correctable by the user.
  - Alternative considered: Aggressive fuzzy matching. Rejected for initial implementation because wrong invoice attachment is worse than a manual correction.

- Use the existing `CreateInvoiceService` for created invoices.
  - Rationale: It already stamps user ownership and creates invoice allocations based on active address contacts.
  - Alternative considered: Insert invoices directly through repositories. Rejected because it could bypass allocation behavior.

## Risks / Trade-offs

- PDF text extraction may differ from visual layout or change when providers update templates -> Keep parser tests based on real Telia/Enefit text samples and return parser warnings instead of silently importing questionable rows.
- Address text may not match database names exactly -> Normalize whitespace/case/punctuation and show unmatched address names so the user can update the DB or import manually.
- A PDF may contain multiple invoices or totals that are hard to assign to one period -> Parser must expose invoice metadata and row-level amounts; imports with missing date or amount must be skipped with a clear error.
- Duplicate imports could create duplicate invoice rows -> First implementation should detect likely duplicates by service, address, invoice date, period, and total amount before creating; unresolved duplicates should be reported as skipped.
- Provider detection may be ambiguous -> The import must fail safely with an unsupported/ambiguous provider message and create no invoices.
- Large or malformed PDFs could consume resources -> Limit upload to PDF content type and a reasonable file size in the Web action before text extraction.
