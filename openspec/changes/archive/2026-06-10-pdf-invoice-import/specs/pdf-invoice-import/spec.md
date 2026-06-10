## ADDED Requirements

### Requirement: PDF import upload
The system SHALL allow an authenticated user to upload one PDF file for invoice import.

#### Scenario: Upload page is available
- **WHEN** an authenticated user opens the invoice import page
- **THEN** the system displays a PDF upload form

#### Scenario: Non-PDF upload is rejected
- **WHEN** an authenticated user submits a file that is not a PDF
- **THEN** the system rejects the import and creates no invoices

#### Scenario: Oversized upload is rejected
- **WHEN** an authenticated user submits a PDF larger than the configured upload limit
- **THEN** the system rejects the import and creates no invoices

### Requirement: Provider detection
The system SHALL detect whether an uploaded PDF is a supported Telia or Enefit invoice before parsing address-level invoice rows.

#### Scenario: Telia invoice is detected
- **WHEN** the uploaded PDF text contains Telia invoice markers
- **THEN** the system selects the Telia import parser

#### Scenario: Enefit invoice is detected
- **WHEN** the uploaded PDF text contains Enefit invoice markers
- **THEN** the system selects the Enefit import parser

#### Scenario: Unsupported provider is rejected
- **WHEN** the uploaded PDF text does not match Telia or Enefit
- **THEN** the system rejects the import and creates no invoices

#### Scenario: Ambiguous provider is rejected
- **WHEN** the uploaded PDF text matches more than one provider with equal confidence
- **THEN** the system rejects the import and creates no invoices

### Requirement: Provider-specific PDF parsing
The system SHALL parse Telia and Enefit invoices using separate provider-specific import processes.

#### Scenario: Multi-page invoice is parsed
- **WHEN** a supported provider PDF contains multiple pages
- **THEN** the system reads all pages before producing invoice rows

#### Scenario: Multiple addresses are parsed
- **WHEN** a supported provider PDF contains charges for multiple addresses
- **THEN** the system produces one parsed invoice row per address charge

#### Scenario: Address amount is preserved
- **WHEN** a parsed invoice row contains an address-specific amount
- **THEN** the system uses that address-specific amount as the invoice total for that address

#### Scenario: Required invoice metadata is missing
- **WHEN** a parsed invoice row is missing service, invoice date, period, address, or amount data required to create an invoice
- **THEN** the system skips that row and reports the missing data

### Requirement: Service matching
The system SHALL match the parsed provider/service name to an existing `Service` record owned by the authenticated user.

#### Scenario: Existing service is matched
- **WHEN** the parsed provider is Telia or Enefit and the current user has a matching service record
- **THEN** the system uses that service when creating imported invoices

#### Scenario: Missing service blocks import
- **WHEN** the parsed provider has no matching service record for the current user
- **THEN** the system creates no invoices and reports the missing service match

#### Scenario: Another user's service is ignored
- **WHEN** another user has a matching service record but the current user does not
- **THEN** the system treats the service as missing for the current user

### Requirement: Address matching
The system SHALL match each parsed PDF address to an existing `Address` record owned by the authenticated user before creating an invoice for that address. Matching SHALL support normalized exact matches and normalized partial matches where the parsed PDF address text is contained in the stored address name or full address.

#### Scenario: Address name is matched
- **WHEN** a parsed PDF address matches the current user's address name after normalization
- **THEN** the system attaches the imported invoice to that address

#### Scenario: Full address is matched
- **WHEN** a parsed PDF address matches the current user's full address after normalization
- **THEN** the system attaches the imported invoice to that address

#### Scenario: Partial address is matched
- **WHEN** a parsed PDF address is contained in exactly one current-user address name or full address after normalization
- **THEN** the system attaches the imported invoice to that address

#### Scenario: Ambiguous partial address is skipped
- **WHEN** a parsed PDF address is contained in more than one equally ranked current-user address
- **THEN** the system skips that address row and reports the ambiguous address match

#### Scenario: Unmatched address is skipped
- **WHEN** a parsed PDF address does not match any current-user address
- **THEN** the system skips that address row and reports the unmatched address

#### Scenario: Another user's address is ignored
- **WHEN** another user has a matching address but the current user does not
- **THEN** the system treats the address as unmatched for the current user

### Requirement: Invoice creation from imported rows
The system SHALL create one `Invoice` per successfully matched address row from the PDF.

#### Scenario: Matched row creates invoice
- **WHEN** a parsed row has a matched service, matched address, invoice date, period, and amount
- **THEN** the system creates an invoice with the parsed service, address, invoice date, period, and amount

#### Scenario: Imported invoice creates allocations
- **WHEN** the system creates an invoice from an imported row
- **THEN** the system applies the same allocation behavior as manual invoice creation

#### Scenario: Partial import succeeds with skipped rows
- **WHEN** a PDF contains both matched and unmatched address rows
- **THEN** the system creates invoices for matched rows and reports skipped rows for unmatched addresses

### Requirement: Duplicate import protection
The system SHALL avoid creating duplicate invoices for rows that already exist for the current user.

#### Scenario: Existing equivalent invoice is skipped
- **WHEN** an imported row matches an existing invoice by service, address, invoice date, period, and total amount
- **THEN** the system skips that row and reports it as an existing invoice

#### Scenario: Different amount is not treated as duplicate
- **WHEN** an imported row has the same service, address, invoice date, and period as an existing invoice but a different total amount
- **THEN** the system does not treat the row as an exact duplicate

### Requirement: Import result reporting
The system SHALL show the user a result summary after an import attempt.

#### Scenario: Successful import summary
- **WHEN** one or more invoices are created from the PDF
- **THEN** the system shows the number of created invoices and their matched addresses and amounts

#### Scenario: Skipped row summary
- **WHEN** one or more parsed rows are skipped
- **THEN** the system shows the skipped rows and the reason each row was skipped

#### Scenario: Fatal import error summary
- **WHEN** provider detection, text extraction, or required service matching fails
- **THEN** the system shows the error and confirms that no invoices were created
