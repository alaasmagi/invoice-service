## Why

Manual invoice entry is slow and error-prone when provider PDFs contain multiple pages and multiple billable addresses. Importing Telia and Enefit invoice PDFs directly will reduce duplicate data entry and ensure generated monthly statements are based on the provider invoice amounts per address.

## What Changes

- Add PDF invoice import for provider invoices whose layout is known and provider-specific.
- Detect the invoice service/provider from the uploaded PDF before parsing address rows.
- Support separate import pipelines for Telia and Enefit because their PDF formats differ.
- Parse provider invoice metadata, invoice dates/periods, address-specific charges, and totals from one or more pages.
- Match parsed provider/service name to an existing `Service` record scoped to the current user.
- Match parsed address names to existing `Address` records scoped to the current user.
- Create one `Invoice` per matched address using the address-specific amount from the PDF.
- Report unmatched services, unmatched addresses, skipped rows, and created invoices to the user before or after import.
- Prevent cross-user data access by matching and creating records only within the authenticated user's data.
- No breaking changes to existing invoice creation or monthly statement generation.

## Capabilities

### New Capabilities
- `pdf-invoice-import`: Upload and import Telia and Enefit invoice PDFs into address-specific invoice records.

### Modified Capabilities
- None.

## Impact

- Domain may need import result/value objects for parsed invoice data and import outcomes.
- Contracts will need PDF import service interfaces and provider parser abstractions.
- Application will orchestrate provider detection, provider-specific parsing, service/address matching, invoice creation, duplicate handling, and result reporting.
- DataAccess may need repository queries for service and address matching, and may require additional invoice metadata if duplicate detection cannot rely on existing fields.
- Web will need an import page, upload form, result view, and navigation from invoices or monthly statements.
- A PDF text extraction dependency will be introduced and must work with multi-page PDFs.
