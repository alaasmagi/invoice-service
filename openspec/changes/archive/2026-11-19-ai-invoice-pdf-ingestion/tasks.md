## 1. Message contract

- [x] 1.1 Add the `invoice-parse` command and the `invoice-parsed` / `invoice-parse-failed` events (routing keys, exchanges, fields, formats, failure reasons) to `docs/contracts/event-contract.md` in the base package
- [x] 1.2 Add the content types to `Contracts/External` (`InvoiceParseCommandContent`, `InvoiceParsedContent` with rows, `InvoiceParseFailedContent`) with explicit `JsonPropertyName` wire names
- [x] 1.3 Add `IInvoiceParseRequestPublisher` to `Contracts/External` (publish one parse request, return a confirm/fail result)
- [x] 1.4 Tests: serialize the command and deserialize sample replies, asserting the exact contract field names and formats

## 2. Domain and persistence

- [x] 2.1 Add `InvoiceDocument` (owner, storage key, file name, size, SHA-256, status, failure reason, parse requested/expires at), `InvoiceDocumentRow` (parsed values, outcome, reason, invoice id) and `ApiKey` (owner, name, hash, prefix, created, last used, revoked) domain entities, plus the status and row-outcome enums
- [x] 2.2 Add nullable `InvoiceDocumentId` to `Invoice`
- [x] 2.3 Add `...Entity` classes with persistence annotations, `IMapper` mappers, `AppDbContext` configuration (unique `(AppUserId, Sha256)` on documents, unique hash on API keys, FKs and delete behavior)
- [x] 2.4 Add `IInvoiceDocumentRepository` and `IApiKeyRepository` (contracts + implementations on the base repositories), including lookups by SHA-256, by id for the reply handler and by key hash
- [x] 2.5 Generate the EF Core migration (do not apply it)
- [x] 2.6 Tests: mappers round-trip all fields; repositories scope by user

## 3. Matching extracted from the PDF import

- [x] 3.1 Move service lookup, address matching, duplicate check and invoice creation out of `PdfInvoiceImportService` into one application service that takes a user id and a parsed document and returns per-row outcomes (`Created`, `Duplicate`, `Unmatched`, `Invalid`)
- [x] 3.2 Make the service unmatched case produce `Unmatched` rows instead of a fatal error, and link created invoices to the document
- [x] 3.3 Tests: port the existing matching cases (exact, full, partial, ambiguous, other user's records, duplicates, missing data) to the new service

## 4. Document pipeline

- [x] 4.1 Add PDF validation (empty, size ≤ 10 MB, `%PDF-` header) and SHA-256 hashing
- [x] 4.2 Add the document upload application service: validate → duplicate check → store with `InvoicePdfStorageKey` → record `Uploaded` → request parsing (4.3)
- [x] 4.3 Add the parse request step: 1-hour signed link, publish the command with `expiresAt` = link expiry, set `Processing` or keep `Uploaded` with the reason on failure; reuse it for retry (`Uploaded`, timed out, `Failed`)
- [x] 4.4 Implement `IInvoiceParseRequestPublisher` in Infrastructure with the ai-hub keyed publisher and `BaseEventEnvelope.CreateCommand`; never log the link
- [x] 4.5 Add the on-demand download link (owner check, signed link ≤ 15 minutes)
- [x] 4.6 Tests: validation cases, duplicate returns existing document without publishing, storage key format, publish failure keeps `Uploaded`, retry sends a fresh link, command expiry equals link expiry, link never logged, download denied for non-owners

## 5. Reply handling

- [x] 5.1 Add the `invoice-parsed` handler: load document, act only when `Processing`, convert content to the parsed document (invalid dates/amounts → `Failed`), run matching, persist rows, set `Completed` or `NeedsReview`
- [x] 5.2 Add the `invoice-parse-failed` handler: set `Failed` with the reason when `Processing`
- [x] 5.3 Register both routes on the routing consumer (`ai-hub`, `invoice-parsed` / `invoice-parse-failed`)
- [x] 5.4 Tests: all matched → `Completed`; partial → `NeedsReview`; duplicate rows; invalid content → `Failed` and acked; failed reply; redelivered, unknown-document and late replies ignored; owner taken from the document

## 6. Review step

- [x] 6.1 Add the review application service: resolve a row with service + address (same creation path, duplicate check), discard a row, complete the document when no unmatched rows remain
- [x] 6.2 Tests: resolve creates invoice with allocations, resolve to duplicate, discard, last row completes, other users' services/addresses rejected

## 7. Web UI

- [x] 7.1 Change the import upload to the new pipeline and redirect to the document page
- [x] 7.2 Add document list and detail pages (status, file, created invoices, row outcomes, retry, download) with Web DTOs and mappers
- [x] 7.3 Add the review page for unmatched rows
- [x] 7.4 Show the PDF download on invoices created from a document
- [x] 7.5 Add the API key settings page (create with show-once, list, revoke)
- [x] 7.6 Check the design folder for these screens first; stop and ask if designs are missing (CLAUDE.md UI rules)

## 8. API key authentication and upload API

- [x] 8.1 Add API key generation (`isk_` + 32 random bytes, base64url), hashing, creation and revocation in an application service
- [x] 8.2 Add the `ApiKey` authentication handler (`X-Api-Key`, 401 without detail, updates last use), applied only to API controllers
- [x] 8.3 Add the named fixed-window rate-limit policy (30/min) partitioned by user (IP fallback) with the built-in limiter
- [x] 8.4 Add `POST /api/v1/invoice-documents` (202 new, 200 duplicate, 400 invalid) returning a Web DTO
- [x] 8.5 Tests: key format and hashing, plain key never stored, revoked/unknown/missing key → 401, API key does not authenticate MVC, endpoint status codes, rate limit 429 per user

## 9. Remove the local parsers

- [x] 9.1 Delete the Telia/Enefit parsers, provider detector, text extractor, their contracts, DI registrations and tests; keep only what the extracted matching still uses
- [x] 9.2 Remove the `UglyToad.PdfPig` package reference

## 10. Wrap-up

- [x] 10.1 `dotnet build` and `dotnet test` pass
- [x] 10.2 Search the repo for any new or changed environment variables and sync `.env`, `.env.example`, the deploy workflow and Docker files (none expected)
- [x] 10.3 Run the macOS backslash cleanup
- [x] 10.4 Change summary with migration and manual steps (apply migration, create an API key, ai-hub must implement the contract before replies are published)
