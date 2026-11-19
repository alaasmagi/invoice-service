## MODIFIED Requirements

### Requirement: PDF import upload
The system SHALL allow an authenticated user to upload one PDF file for invoice import. An accepted upload SHALL be
stored as an invoice document and queued for AI parsing; invoices are created when the parse result arrives.

#### Scenario: Upload page is available
- **WHEN** an authenticated user opens the invoice import page
- **THEN** the system displays a PDF upload form

#### Scenario: Accepted upload is queued
- **WHEN** an authenticated user submits a valid PDF
- **THEN** the system stores it as an invoice document, requests AI parsing and shows the document's status page

#### Scenario: Non-PDF upload is rejected
- **WHEN** an authenticated user submits a file that is not a PDF
- **THEN** the system rejects the import and creates no invoices

#### Scenario: Oversized upload is rejected
- **WHEN** an authenticated user submits a PDF larger than the configured upload limit
- **THEN** the system rejects the import and creates no invoices

### Requirement: Service matching
The system SHALL match the parsed provider/service name to an existing `Service` record owned by the document's
owner. When no service matches, the document's rows SHALL wait for the user to choose a service in review.

#### Scenario: Existing service is matched
- **WHEN** the parsed service name matches a service record of the document's owner
- **THEN** the system uses that service when creating imported invoices

#### Scenario: Missing service requires review
- **WHEN** the parsed service name has no matching service record for the document's owner
- **THEN** the system creates no invoices yet, records the rows as unmatched and sets the document to `NeedsReview`

#### Scenario: Another user's service is ignored
- **WHEN** another user has a matching service record but the document's owner does not
- **THEN** the system treats the service as missing for the document's owner

### Requirement: Address matching
The system SHALL match each parsed PDF address to an existing `Address` record owned by the document's owner before
creating an invoice for that address. Matching SHALL support normalized exact matches and normalized partial matches
where the parsed PDF address text is contained in the stored address name or full address. Rows that cannot be
matched SHALL wait for the user to choose an address in review.

#### Scenario: Address name is matched
- **WHEN** a parsed PDF address matches the owner's address name after normalization
- **THEN** the system attaches the imported invoice to that address

#### Scenario: Full address is matched
- **WHEN** a parsed PDF address matches the owner's full address after normalization
- **THEN** the system attaches the imported invoice to that address

#### Scenario: Partial address is matched
- **WHEN** a parsed PDF address is contained in exactly one of the owner's address names or full addresses after normalization
- **THEN** the system attaches the imported invoice to that address

#### Scenario: Ambiguous partial address requires review
- **WHEN** a parsed PDF address is contained in more than one equally ranked address of the owner
- **THEN** the system records the row as unmatched with the ambiguity reason and the document requires review

#### Scenario: Unmatched address requires review
- **WHEN** a parsed PDF address does not match any address of the owner
- **THEN** the system records the row as unmatched and the document requires review

#### Scenario: Another user's address is ignored
- **WHEN** another user has a matching address but the document's owner does not
- **THEN** the system treats the address as unmatched for the document's owner

### Requirement: Invoice creation from imported rows
The system SHALL create one `Invoice` per successfully matched address row of a parsed document, linked to that
document.

#### Scenario: Matched row creates invoice
- **WHEN** a parsed row has a matched service, matched address, invoice date, period, and amount
- **THEN** the system creates an invoice with the parsed service, address, invoice date, period, and amount, linked to the document

#### Scenario: Imported invoice creates allocations
- **WHEN** the system creates an invoice from an imported row
- **THEN** the system applies the same allocation behavior as manual invoice creation

#### Scenario: Partial import goes to review
- **WHEN** a parsed document contains both matched and unmatched rows
- **THEN** the system creates invoices for matched rows and sets the document to `NeedsReview` for the rest

#### Scenario: Row with missing data is not created
- **WHEN** a parsed row is missing the address, amount or invoice date needed to create an invoice
- **THEN** the system creates no invoice for that row and records it as invalid with the missing data

### Requirement: Import result reporting
The system SHALL show the user each import's outcome on the invoice document's page once parsing has finished.

#### Scenario: Successful import summary
- **WHEN** one or more invoices are created from the document
- **THEN** the document page shows the number of created invoices and their matched addresses and amounts

#### Scenario: Skipped row summary
- **WHEN** one or more parsed rows are duplicates, invalid, unmatched or discarded
- **THEN** the document page shows those rows and the reason for each

#### Scenario: Failed import summary
- **WHEN** parsing fails or the parse result cannot be used
- **THEN** the document page shows the failure reason and confirms that no invoices were created

## REMOVED Requirements

### Requirement: Provider detection
**Reason**: Provider-specific detection is replaced by AI parsing in ai-hub, which handles any provider.
**Migration**: Upload the PDF as before; ai-hub returns the service name, which goes through service matching.

### Requirement: Provider-specific PDF parsing
**Reason**: The Telia and Enefit parsers are removed; ai-hub extracts multi-page, multi-address invoices into the
same parsed-document shape.
**Migration**: No user action. Missing-data handling moves to "Invoice creation from imported rows".
