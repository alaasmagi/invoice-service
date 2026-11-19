## Why

PDF import only understands Telia and Enefit layouts, so every other provider's invoice has to be typed in by hand,
and each new provider means another hand-written parser. Handing the PDF to ai-hub for structured extraction removes
the per-provider parsers, and keeping the original PDF in R2 lets the user download it next to the invoices created
from it. A per-user API key lets a Gmail forwarder push invoice PDFs in without the user opening the app.

## What Changes

- **BREAKING**: Remove the Telia and Enefit parsers, provider detection and local PDF text extraction. Every
  uploaded PDF is parsed by ai-hub instead, so imports become asynchronous: the upload returns at once and the
  invoices appear when ai-hub replies.
- Store every accepted PDF in the private R2 bucket under `{appUserId}/{year}/{sha256}.pdf`, and record it as an
  invoice document (owner, object key, original file name, status, created invoices). Only the key is stored, never
  a URL. Uploading the same file again does not store or parse it twice.
- Track each document through `Uploaded → Processing → Completed | NeedsReview | Failed`, and show the status in
  the UI.
- After storing, publish an `invoice-parse` command to ai-hub with the document id and a time-limited signed link
  to the PDF. The command expires when the link does.
- Consume ai-hub's `invoice-parsed` and `invoice-parse-failed` events from the service queue, and create invoices
  from the parsed rows with the existing service and address matching, duplicate protection and allocation.
- Rows whose service or address cannot be matched put the document in `NeedsReview`, where the user picks the
  service/address (or discards the row) and the invoice is created.
- Let the user download the original PDF through a short-lived signed link created on demand.
- Add per-user API keys: created on a settings page, shown once, stored only as a hash, revocable.
- Add `POST /api/v1/invoice-documents`, authenticated by API key and rate-limited, that runs the same pipeline as
  the UI upload.
- Add the `invoice-parse` command and the two reply events to the platform event contract in the base package.

## Capabilities

### New Capabilities
- `invoice-documents`: storing uploaded invoice PDFs, document status tracking, de-duplication, on-demand
  download links, and the review step for rows that could not be matched.
- `ai-invoice-parsing`: the asynchronous exchange with ai-hub — the parse command with its signed link and expiry,
  consuming the parsed/failed reply events, and creating invoices from the parsed rows.
- `api-key-authentication`: per-user API key lifecycle (create, show once, hash, revoke) and authenticating
  requests by API key.
- `invoice-document-upload-api`: the rate-limited API endpoint a Gmail forwarder posts PDFs to.

### Modified Capabilities
- `pdf-invoice-import`: provider detection and provider-specific parsing are removed; the upload stores the
  document and queues AI parsing instead of creating invoices synchronously; the result summary becomes the
  document's status page; unmatched rows go to review instead of only being reported.

## Impact

- **Removed code**: `TeliaPdfInvoiceParser`, `EnefitPdfInvoiceParser`, `PdfInvoiceParserBase`,
  `PdfInvoiceProviderDetector`, `PdfInvoiceTextExtractor`, `PdfInvoiceNormalization` (where unused after the
  change), their contracts and tests, and the `UglyToad.PdfPig` package.
- **New persistence**: `InvoiceDocument`, `InvoiceDocumentRow` and `ApiKey` entities, a link from `Invoice` to its
  document, and one EF Core migration (applied manually).
- **Messaging**: publishes to `ai-hub.commands` with the existing ai-hub publisher; adds an ai-hub route to the
  routing consumer on the `invoice-service` queue. The ai-hub route must be live before ai-hub publishes replies.
- **Web**: changed import upload and result views, new document list/detail/review views, an API key settings
  page, the first controller under `Web/Controllers/Api`, and the first rate-limit policy.
- **Base package**: `docs/contracts/event-contract.md` gains the three messages; no code change.
- **Out of scope**: the ai-hub side (download, model call, publishing replies) and the Gmail forwarder itself
  (e.g. Google Apps Script).
- **Privacy**: invoice PDFs (names, addresses, possibly account numbers) are passed to the AI model provider
  through ai-hub.
