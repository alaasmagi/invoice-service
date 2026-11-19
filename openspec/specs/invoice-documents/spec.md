# invoice-documents Specification

## Purpose
Define how uploaded invoice PDFs are stored, tracked through parsing, downloaded and reviewed when rows cannot be matched.
## Requirements
### Requirement: Invoice PDF storage
The system SHALL store every accepted invoice PDF in the private object store under the key
`{appUserId}/{year}/{sha256}.pdf`, where `year` is the UTC upload year and `sha256` is the lower-case hex SHA-256 of
the file. The system SHALL persist only the object key, never a URL.

#### Scenario: Accepted PDF is stored
- **WHEN** a user's PDF passes validation
- **THEN** the system stores the file under that user's key and records an invoice document with the key, original file name, size and SHA-256

#### Scenario: No URL is persisted
- **WHEN** an invoice document is recorded
- **THEN** the stored record contains the object key and no absolute or signed URL

### Requirement: Invoice PDF validation
The system SHALL accept only PDF files of at most 10 MB, identified by the `%PDF-` header, for both the UI upload
and the API upload.

#### Scenario: Non-PDF is rejected
- **WHEN** an uploaded file does not start with the `%PDF-` header
- **THEN** the system rejects it, stores nothing and records no document

#### Scenario: Oversized file is rejected
- **WHEN** an uploaded file is larger than 10 MB
- **THEN** the system rejects it, stores nothing and records no document

#### Scenario: Empty file is rejected
- **WHEN** an uploaded file is empty
- **THEN** the system rejects it, stores nothing and records no document

### Requirement: Duplicate document detection
The system SHALL treat a file whose SHA-256 matches an existing document of the same user as a duplicate and return
the existing document instead of storing or parsing the file again.

#### Scenario: Same file uploaded twice
- **WHEN** a user uploads a PDF with the same content as one of their existing documents
- **THEN** the system returns the existing document and sends no new parse request

#### Scenario: Same file uploaded by another user
- **WHEN** another user uploads a PDF with the same content
- **THEN** the system records a separate document for that user under that user's key

### Requirement: Document status tracking
The system SHALL track each invoice document as `Uploaded`, `Processing`, `Completed`, `NeedsReview` or `Failed`,
and show each of the user's documents with its status, file name, upload time and created invoices.

#### Scenario: Document waits for parsing
- **WHEN** the parse request for a document has been sent
- **THEN** the document is shown as `Processing`

#### Scenario: Parse request could not be sent
- **WHEN** storing succeeded but the parse request could not be sent
- **THEN** the document stays `Uploaded`, shows the failure reason and offers a retry

#### Scenario: Parsing timed out
- **WHEN** a document is still `Processing` after its parse request expired
- **THEN** the document is shown as timed out and offers a retry

#### Scenario: Retry sends a new request
- **WHEN** the user retries an `Uploaded`, timed-out or `Failed` document
- **THEN** the system sends a new parse request with a fresh link and sets the document to `Processing`

#### Scenario: Other users' documents are hidden
- **WHEN** a user lists or opens invoice documents
- **THEN** the system shows only documents owned by that user

### Requirement: Original PDF download
The system SHALL let the owner download a document's original PDF through a signed link created at request time
that expires within 15 minutes.

#### Scenario: Owner downloads the PDF
- **WHEN** the owner chooses to download a document's PDF
- **THEN** the system creates a short-lived signed link and redirects the browser to it

#### Scenario: Non-owner cannot download
- **WHEN** a user requests the PDF of a document they do not own
- **THEN** the system responds as if the document does not exist and creates no link

#### Scenario: Invoice links to its PDF
- **WHEN** a user views an invoice that was created from a document
- **THEN** the system offers the original PDF download for that invoice

### Requirement: Review of unmatched rows
The system SHALL put a document in `NeedsReview` when any parsed row could not be matched to the user's service or
address, and SHALL let the user resolve each such row by choosing a service and address or by discarding it.

#### Scenario: Unmatched rows require review
- **WHEN** a parsed document has at least one row with no matching service or address
- **THEN** the system creates invoices for matched rows and sets the document to `NeedsReview`

#### Scenario: User resolves a row
- **WHEN** the user picks a service and address for an unmatched row
- **THEN** the system creates the invoice through the normal invoice creation and allocation rules and marks the row created

#### Scenario: Resolved row duplicates an existing invoice
- **WHEN** the user resolves a row to a service and address for which an equivalent invoice already exists
- **THEN** the system creates no invoice and marks the row as a duplicate

#### Scenario: User discards a row
- **WHEN** the user discards an unmatched row
- **THEN** the system creates no invoice for it and marks the row discarded

#### Scenario: Last row resolved completes the document
- **WHEN** no unmatched rows remain on a document in `NeedsReview`
- **THEN** the system sets the document to `Completed`

#### Scenario: Review offers only the user's records
- **WHEN** the review page lists services and addresses to choose from
- **THEN** it lists only the current user's services and addresses

