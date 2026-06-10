## 1. Contracts and Models

- [x] 1.1 Add import result/value models for parsed provider, parsed invoice rows, created invoices, skipped rows, warnings, and fatal errors.
- [x] 1.2 Add an application service interface for PDF invoice import.
- [x] 1.3 Add parser abstraction interfaces for provider detection and provider-specific invoice parsing.
- [x] 1.4 Add repository query methods needed for service matching, address matching, and duplicate invoice detection.

## 2. PDF Extraction and Provider Detection

- [x] 2.1 Add a PDF text extraction package compatible with .NET 10 and wire it into the Application layer.
- [x] 2.2 Implement a PDF text extraction service that reads text from every page.
- [x] 2.3 Implement provider detection for Telia and Enefit markers.
- [x] 2.4 Return safe fatal errors when extraction fails, no provider is detected, or provider detection is ambiguous.

## 3. Provider Parsers

- [x] 3.1 Implement the Telia parser for multi-page PDFs and multiple address charge rows.
- [x] 3.2 Implement the Enefit parser for multi-page PDFs and multiple address charge rows.
- [x] 3.3 Parse invoice date, period start, period end, service/provider name, PDF address text, and address-specific amount for each row.
- [x] 3.4 Skip parser rows with missing required data and include row-level reasons in the import result.
- [x] 3.5 Add parser fixtures or text samples derived from the provided Telia and Enefit examples.

## 4. Matching and Import Orchestration

- [x] 4.1 Implement normalized service matching against current-user `Service.Name`.
- [x] 4.2 Implement normalized address matching against current-user `Address.Name` and `Address.FullAddress`.
- [x] 4.3 Ensure matching never uses services or addresses owned by another user.
- [x] 4.4 Implement duplicate detection by current user, service, address, invoice date, period, and total amount.
- [x] 4.5 Create invoices for matched non-duplicate rows through `CreateInvoiceService` so allocations are generated consistently.
- [x] 4.6 Support partial imports where matched rows are created and unmatched or duplicate rows are reported as skipped.

## 5. Web UI

- [x] 5.1 Add an MVC controller action pair for PDF import upload and result display.
- [x] 5.2 Add a PDF upload view with validation messaging for unsupported file types and size limits.
- [x] 5.3 Add an import result view showing created invoices, matched addresses, amounts, skipped rows, warnings, and fatal errors.
- [x] 5.4 Add navigation to the import page from the invoices area.
- [x] 5.5 Keep all upload and import actions authenticated and scoped to the current user.

## 6. Verification

- [x] 6.1 Add unit tests for provider detection.
- [x] 6.2 Add unit tests for Telia parser behavior using sample extracted text.
- [x] 6.3 Add unit tests for Enefit parser behavior using sample extracted text.
- [x] 6.4 Add tests for normalized service and address matching, including another-user records.
- [x] 6.5 Add tests for duplicate detection and partial import result reporting.
- [x] 6.6 Run build and relevant test suite, or document any environment blocker.
