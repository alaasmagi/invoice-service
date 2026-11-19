## Context

`PdfInvoiceImportService` extracts text with PdfPig, detects Telia or Enefit, runs a hand-written parser into a
`PdfInvoiceParsedDocument` (service name, dates, rows of address text + amount), then matches the service and each
address to the user's records, skips duplicates and creates invoices through `ICreateInvoiceService` — all inside
the upload request. The PDF itself is discarded.

Pieces already in place for this change:
- `IFileStorage` (`Contracts/External`) backed by the private R2 bucket, with signed GET links, and
  `InvoicePdfStorageKey.Build(appUserId, year, sha256)`.
- A keyed `IBaseEventPublisher` for `ai-hub.commands` (`RabbitMqPublisherKeys.AiHub`).
- The base package 1.7.0 routing consumer on the `invoice-service` queue, which is already bound to
  `ai-hub.events` with `ai-hub.invoice-service.#`. Today it only routes `identity-hub`; anything else is rejected
  and, without a dead-letter exchange, lost.
- Base package 1.6+ envelopes support commands with `expiresAt` (`BaseEventEnvelope<T>.CreateCommand`).

Constraints: CLAUDE.md layering (contracts provider-agnostic, domain entities between layers, mapping at the
edges), env variables only in `.env`, migrations applied by hand, `Web/Controllers/Api` is empty and the app has no
rate limiting yet. The ai-hub side is built separately against the contract defined here.

## Goals / Non-Goals

**Goals:**
- One pipeline for both entry points: validate → store → record → ask ai-hub → create invoices on reply.
- Keep the PDF and link it to the invoices made from it.
- Reuse the existing service/address matching, duplicate protection and allocation unchanged.
- Never lose an upload: every failure leaves a document the user can see and retry.

**Non-Goals:**
- The ai-hub implementation (download, prompt, model, reply publishing).
- The Gmail forwarder (Apps Script or similar).
- Editing parsed amounts or dates in the review step; review only resolves service/address matches or discards rows.
- Re-parsing documents uploaded before this change (none are stored).

## Decisions

### D1. Store the object key; links are always generated
`InvoiceDocument.StorageKey` holds `{appUserId}/{year}/{sha256}.pdf`, where `year` is the upload year (UTC) because
the invoice date is unknown until ai-hub replies. The bucket stays private; downloads and ai-hub both get signed
links. *Alternative:* a public bucket or stored URL — rejected, it exposes invoices and ties the DB to one host.

### D2. De-duplicate by content hash per user
SHA-256 of the bytes is stored on the document with a unique index on `(AppUserId, Sha256)`. Uploading a file the
user already has returns the existing document instead of storing or parsing it again. Because R2 writes are
overwrites of identical bytes, a crash between storing and recording is harmless: the retry writes the same key.

### D3. Order of operations and the `Uploaded` state
1. Validate (size ≤ 10 MB, `%PDF-` header); 2. hash; 3. upload to R2; 4. insert the document as `Uploaded` and
commit; 5. publish the command; 6. set `Processing` with `ParseRequestedAt` and `ParseExpiresAt`.
If publishing fails the document stays `Uploaded` with the failure reason, and the user can retry. *Alternative:*
a transactional outbox — more robust, but there is no outbox infrastructure yet; the explicit `Uploaded` state plus
retry covers a single-user app. Revisit if uploads become high-volume.

### D4. Signed link lifetime = command lifetime = 1 hour
The command carries the link and `expiresAt` equal to the link's expiry, so ai-hub drops commands it can no longer
fulfil. A document still `Processing` after `ParseExpiresAt` is shown as timed out and can be retried, which sends a
new command with a fresh link. 15 minutes is too short to survive an ai-hub restart or backlog; much longer widens
the window in which a leaked link works.

### D5. Message contract (added to the base package's `event-contract.md`)
- Command `invoice-service.invoice-service.invoice-parse` → `ai-hub.commands`, `expiresAt` set (D4). Content:
  `documentId` (UUID string), `fileUrl`, `fileName`, `contentType` (`application/pdf`), `sha256`.
- Event `ai-hub.invoice-service.invoice-parsed` → `ai-hub.events`. Content: `documentId`, `serviceName`,
  `invoiceDate`, `periodStart`, `periodEnd` (`yyyy-MM-dd` or null), `currency` (ISO 4217), and `rows[]` with
  `addressText`, `amount` (invariant decimal string), optional per-row `invoiceDate`/`periodStart`/`periodEnd`.
  This mirrors `PdfInvoiceParsedDocument`, so the existing matching code consumes it unchanged.
- Event `ai-hub.invoice-service.invoice-parse-failed` → `ai-hub.events`. Content: `documentId`, `reason`
  (`download-failed`, `not-an-invoice`, `unreadable`, `model-error`, `expired`) and a short `message`.
- The command carries no user id or personal data beyond what is in the PDF; the document id is the correlation key.

### D6. Reply handling is idempotent and owner-scoped
Two typed handlers are routed from the routing consumer (`Route<InvoiceParsedContent>("ai-hub", "invoice-parsed")`,
`Route<InvoiceParseFailedContent>("ai-hub", "invoice-parse-failed")`). They load the document by id and act only if
it is `Processing`; replies for unknown, completed or failed documents are acked and ignored (redelivery and late
replies are harmless). The owner comes from the document, never from the message. Validation failures in the
content (bad dates or amounts) mark the document `Failed` rather than throwing, so the message is not rejected.

### D7. Extract matching into a reusable application service
The service lookup, address matching, duplicate check and invoice creation move out of `PdfInvoiceImportService`
into one service that takes a user id and a `PdfInvoiceParsedDocument` and returns per-row outcomes
(`Created`, `Duplicate`, `Unmatched`, `Invalid`). The reply handler and the review step both call it. Each parsed
row is persisted as an `InvoiceDocumentRow` with its outcome and, when created, the invoice id; invoices also get a
nullable `InvoiceDocumentId`.

### D8. Review resolves matches, not values
A document with any `Unmatched` row is `NeedsReview`. The review page shows those rows with the parsed text; the user
picks the service (once per document) and/or an address per row, or discards the row. Resolved rows run through the
same creation path (including the duplicate check). When no unmatched rows remain the document becomes `Completed`.

### D9. API keys: random, hashed, prefixed
Keys are 32 random bytes, base64url-encoded, with an `isk_` prefix so they are recognisable in logs and secret
scanners. Only a SHA-256 hash is stored (the key's entropy makes a slow hash unnecessary), plus a short display
prefix, name, `CreatedAt`, `LastUsedAt` and `RevokedAt`. The full key is shown once on creation. A user can have
several keys and revoke each.

### D10. Separate authentication scheme for the API
CLAUDE.md §8 allows only Keycloak JWT bearer on API endpoints; the owner approved API keys for this endpoint as a
one-off exception, because the Gmail forwarder cannot run an interactive Keycloak login. A custom `ApiKey` authentication handler reads the `X-Api-Key` header, looks up the hash, rejects revoked keys and
sets the owning user's id as the principal. It applies only to controllers under `Web/Controllers/Api`; the MVC
side keeps Keycloak OIDC. Failed lookups return 401 without saying whether the key existed.

### D11. Rate limiting with the built-in limiter
ASP.NET Core's `Microsoft.AspNetCore.RateLimiting` (part of the shared framework, no new package) with one named
fixed-window policy of 30 requests per minute partitioned by the authenticated user (or client IP when
unauthenticated), applied to every API controller.
This becomes the "existing named rate-limit policy" later API controllers reuse.

### D12. Endpoint shape
`POST /api/v1/invoice-documents` (multipart, field `file`) returns `202 Accepted` with the document id and status, or
`200 OK` with the existing document for a duplicate upload; `400` for invalid files, `401` without a valid key,
`429` when limited. Responses are Web DTOs, never domain entities.

## Risks / Trade-offs

- [ai-hub is not built yet, so every upload stays `Processing`] → Deploy behind the review/status UI so users see
  it; ship invoice-service first, then enable ai-hub publishing; documents can be retried once ai-hub is live.
- [ai-hub replies before the route exists and messages are lost] → The routes ship in this change; ai-hub must not
  publish to `ai-hub.invoice-service.*` before it is deployed. A dead-letter exchange on the queue is recommended.
- [Model returns plausible but wrong amounts] → Amounts are created as parsed; the document links to the PDF so the
  user can check. Editing parsed values is a non-goal for now.
- [Signed link leaks] → 1-hour lifetime, bucket stays private, links are never logged or persisted.
- [Personal data reaches the AI provider] → Documented in the proposal; the provider's retention and training terms
  must be acceptable before enabling ai-hub.
- [Losing the synchronous result page] → **BREAKING** for users: results arrive later. The document page replaces
  the immediate summary.
- [Publish fails after storing] → Document stays `Uploaded` with a retry action (D3).

## Migration Plan

1. Apply the EF migration (new tables, nullable `Invoice.InvoiceDocumentId`) by hand.
2. Add `STORAGE_*` values and the R2 API token if not done; no new environment variables are expected.
3. Deploy invoice-service; the old parsers are gone, uploads queue for ai-hub.
4. Publish the contract change in the base package docs; build and deploy ai-hub against it.
5. Rollback: redeploy the previous image; the new tables and nullable column are unused by the old code.

## Resolved Questions

- Rate limit: 30 requests per minute per user.
- Stored PDFs are permanent; documents cannot be deleted in this change.
- The parsed `currency` is ignored; amounts are used as parsed, as with manual invoices.
