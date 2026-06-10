## ADDED Requirements

### Requirement: Monthly statements are person-month aggregates
A `MonthlyStatement` SHALL represent one contact/person for one calendar month across all addresses where that person has invoice responsibility. The unique logical statement identity SHALL be the authenticated user, contact, year, and month.

#### Scenario: Person has invoices at one address
- **WHEN** a contact is active at one address with invoices in March 2026
- **THEN** generation SHALL create one March 2026 monthly statement for that contact
- **THEN** the statement SHALL reference that contact and not be limited to an address-level aggregate

#### Scenario: Person has invoices at multiple addresses
- **WHEN** a contact is active at two addresses that both have invoices in March 2026
- **THEN** generation SHALL create one March 2026 monthly statement for the contact
- **THEN** the statement SHALL include lines for invoices from both addresses

### Requirement: Statement lines include address, service, invoice, and share details
Each person-month statement SHALL include line items for the invoice shares that make up the person's total. Each line SHALL identify the address, service, invoice, invoice date/period, invoice total, resident count used for splitting, and the person's allocated amount.

#### Scenario: Statement contains invoice line details
- **WHEN** a generated statement includes an electricity invoice for an address
- **THEN** the statement line SHALL identify the address
- **THEN** the statement line SHALL identify the service name/source through the invoice service
- **THEN** the statement line SHALL include the full invoice total and the person's allocated share

### Requirement: Address invoices are split evenly among active residents
For each invoice in the target month, the system SHALL find all active `AddressContact` relationships for the invoice address and split the invoice total evenly among those contacts. Any rounding remainder SHALL be assigned deterministically so all person shares sum exactly to the invoice total.

#### Scenario: Two active residents split an invoice equally
- **WHEN** an address has a 100.00 invoice and two active contacts for that invoice period
- **THEN** each contact's monthly statement SHALL receive a line with 50.00 allocated amount

#### Scenario: Three active residents split with rounding remainder
- **WHEN** an address has a 100.00 invoice and three active contacts for that invoice period
- **THEN** three statement lines SHALL be created with allocated amounts that sum exactly to 100.00
- **THEN** the rounding remainder SHALL be assigned using deterministic contact ordering

#### Scenario: No active residents for invoice address
- **WHEN** an invoice exists for an address with no active contacts for that invoice period
- **THEN** generation SHALL not create a person statement line for that invoice
- **THEN** generation SHALL surface or log that the invoice could not be allocated

### Requirement: Person statements aggregate totals across all lines
The person's monthly statement total SHALL equal the sum of all statement line allocated amounts for that contact, year, and month.

#### Scenario: Person has lines from multiple addresses
- **WHEN** a person has statement lines of 40.00 from Address A and 65.00 from Address B in March 2026
- **THEN** the person's March 2026 monthly statement total SHALL be 105.00

### Requirement: Generation creates or refreshes all person statements for a month
Monthly statement generation SHALL create or refresh person-month statements and lines for the requested month based on invoices and active address-contact relationships owned by the authenticated user.

#### Scenario: Generate month with multiple people
- **WHEN** March 2026 has invoices for addresses with three distinct active contacts
- **THEN** generation SHALL create or update one March 2026 statement for each distinct contact with allocated invoice shares

#### Scenario: Regenerate existing person statement
- **WHEN** a person-month statement already exists and generation is run again for the same month
- **THEN** the system SHALL recalculate the statement lines and total from current invoice and address-contact data
- **THEN** stale lines that no longer apply SHALL be removed or replaced

### Requirement: Statement UI displays person-centered breakdown
The Monthly Statement details page SHALL show the contact/person name, year/month, total amount, send/payment status, all addresses represented, and invoice line details including service names and allocated shares.

#### Scenario: View person monthly statement details
- **WHEN** a user opens a person-month statement details page
- **THEN** the page SHALL show the person's name
- **THEN** the page SHALL show all statement lines with address, service, invoice date/period, invoice total, and the person's share

### Requirement: Send action sends one email per person-month statement
Sending a monthly statement SHALL send one email to the statement contact for that person/month statement and SHALL include all statement lines in that email.

#### Scenario: Person has multiple address lines
- **WHEN** a person-month statement includes invoice lines from multiple addresses
- **THEN** the Send action SHALL send one email to that person
- **THEN** the email SHALL include every statement line from those addresses
