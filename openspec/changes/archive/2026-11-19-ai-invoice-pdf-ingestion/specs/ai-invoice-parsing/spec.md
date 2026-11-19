## ADDED Requirements

### Requirement: Parse request to ai-hub
After storing a document, the system SHALL publish an `invoice-parse` command to the ai-hub commands exchange with
routing key `invoice-service.invoice-service.invoice-parse`, carrying the document id, a signed GET link to the PDF,
the file name, the content type and the SHA-256. The command's `expiresAt` SHALL equal the link's expiry, one hour
after sending.

#### Scenario: Command is published after storing
- **WHEN** a new document has been stored and recorded
- **THEN** the system publishes one `invoice-parse` command whose `documentId` is that document's id

#### Scenario: Command expires with its link
- **WHEN** the system publishes an `invoice-parse` command
- **THEN** the command's `expiresAt` equals the expiry of the signed link it carries

#### Scenario: Unconfirmed publish leaves the document uploaded
- **WHEN** the broker does not confirm the `invoice-parse` command
- **THEN** the document stays `Uploaded` with the failure reason

#### Scenario: Signed link is not logged
- **WHEN** the system publishes or logs anything about a parse request
- **THEN** no log entry contains the signed link

### Requirement: Parsed invoice reply handling
The system SHALL consume `ai-hub.invoice-service.invoice-parsed` events from its service queue and create invoices
from the parsed rows for the document's owner, using the service matching, address matching, duplicate protection
and allocation rules of PDF invoice import.

#### Scenario: All rows match
- **WHEN** an `invoice-parsed` event arrives for a `Processing` document and every row matches
- **THEN** the system creates one invoice per row, links each to the document and sets the document to `Completed`

#### Scenario: Some rows do not match
- **WHEN** an `invoice-parsed` event arrives and at least one row has no matching service or address
- **THEN** the system creates invoices for matched rows, records the unmatched rows and sets the document to `NeedsReview`

#### Scenario: Duplicate rows are skipped
- **WHEN** a parsed row matches an existing invoice by service, address, invoice date, period and amount
- **THEN** the system creates no invoice for that row and records it as a duplicate

#### Scenario: Owner comes from the document
- **WHEN** the system creates invoices from a reply
- **THEN** it creates them for the owner of the referenced document, regardless of any user data in the message

#### Scenario: Invalid reply content fails the document
- **WHEN** an `invoice-parsed` event has unreadable dates or amounts
- **THEN** the system creates no invoices, sets the document to `Failed` with the reason and acknowledges the message

### Requirement: Failed parse reply handling
The system SHALL consume `ai-hub.invoice-service.invoice-parse-failed` events and set the referenced `Processing`
document to `Failed` with the reported reason.

#### Scenario: ai-hub reports failure
- **WHEN** an `invoice-parse-failed` event arrives for a `Processing` document
- **THEN** the system sets the document to `Failed`, stores the reason and creates no invoices

### Requirement: Idempotent reply handling
The system SHALL act on a reply only when the referenced document exists and is `Processing`, and SHALL acknowledge
and ignore any other reply.

#### Scenario: Redelivered reply
- **WHEN** an `invoice-parsed` event arrives for a document that is already `Completed` or `NeedsReview`
- **THEN** the system creates no invoices and leaves the document unchanged

#### Scenario: Reply for an unknown document
- **WHEN** a reply references a document id that does not exist
- **THEN** the system acknowledges the message and changes nothing

#### Scenario: Late reply after a retry was sent
- **WHEN** a reply for an earlier parse request arrives after the document was retried and is `Processing`
- **THEN** the system processes it once, and any later reply for that document is ignored

### Requirement: Message contract
The `invoice-parse` command and the `invoice-parsed` and `invoice-parse-failed` events SHALL be documented in the
platform event contract with their routing keys, exchanges and content fields, using UUID strings for ids,
`yyyy-MM-dd` for dates, invariant decimal strings for amounts and ISO 4217 currency codes.

#### Scenario: Contract matches the implementation
- **WHEN** the invoice-service serializes the command or deserializes either event
- **THEN** the JSON field names and formats match the documented contract
