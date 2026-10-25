## Purpose
Define person/month statement generation, invoice splitting, statement contents and send behavior.
## Requirements
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
Each person-month statement SHALL include line items for the invoice shares that make up the person's total. Each line SHALL record:
- the address, service and invoice
- the invoice date or period
- the invoice total
- the number of people sharing the invoice (`ResidentCount`)
- the allocation method used (`AllocationMethod`)
- the rule used, if any (`AllocationRuleId`)
- the person's share value (`ShareValue`): the effective percentage for Percentage, the fixed amount for a fixed payer, and empty for Equal, Manual and Fixed-remainder lines
- the person's allocated amount

#### Scenario: Statement contains invoice line details
- **WHEN** a generated statement includes an electricity invoice for an address
- **THEN** the statement line SHALL identify the address
- **THEN** the statement line SHALL identify the service name/source through the invoice service
- **THEN** the statement line SHALL include the full invoice total and the person's allocated share

#### Scenario: Statement line records how the share was calculated
- **WHEN** a line is generated from a Percentage rule where the person's effective share is 60 %
- **THEN** the line SHALL store `AllocationMethod` = Percentage, the rule's id, and `ShareValue` = 60.00

#### Scenario: Email payload stays wire-compatible
- **WHEN** a statement whose lines come from any allocation method is sent
- **THEN** each email row's `fractions` SHALL equal the line's `ResidentCount` and `invoiceFraction` SHALL equal the allocated amount
- **THEN** the JSON shape SHALL be unchanged

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

### Requirement: Address invoices are split according to the resolved allocation method
For each invoice in the target month, the system SHALL decide how to split it using the precedence defined by the `invoice-allocation-rules` capability (manual allocations, then a service rule, then an address default rule, then an equal split). It SHALL then calculate per-person shares with that method. The shares for each allocated invoice SHALL add up exactly to the invoice total.

#### Scenario: Two active residents split an invoice equally without rules
- **WHEN** an address with no rules has a 100.00 invoice and two active contacts for that invoice period
- **THEN** each contact's monthly statement SHALL receive a line with an allocated amount of 50.00

#### Scenario: Three active residents split with rounding remainder
- **WHEN** an address with no rules has a 100.00 invoice and three active contacts for that invoice period
- **THEN** three statement lines SHALL be created with allocated amounts that add up exactly to 100.00
- **THEN** the rounding remainder SHALL be assigned using deterministic contact ordering

#### Scenario: Configured rule drives the split
- **WHEN** an address has a valid Percentage rule of 60/40 for its two residents and a 50.00 invoice
- **THEN** the residents' statements SHALL receive lines of 30.00 and 20.00

### Requirement: Generation reports invoices it could not allocate
Monthly statement generation SHALL return the generated statements together with a list of allocation warnings. Each warning SHALL identify the invoice (address, service and date), state a reason code, and include any parameters needed to display a localized message. Invoices that cannot be allocated SHALL produce no statement lines. The Web layer SHALL show the warnings on the monthly statements page after generation.

#### Scenario: No active residents for invoice address
- **WHEN** an invoice exists for an address with no active contacts for that invoice period
- **THEN** generation SHALL not create a person statement line for that invoice
- **THEN** generation SHALL return a warning for that invoice with reason "no active residents"

#### Scenario: Warnings shown after generation
- **WHEN** the user generates March 2026 and two invoices could not be allocated
- **THEN** the monthly statements page SHALL show an `alert--warning` listing both invoices with localized reasons and links to fix them (residents, split rule or manual split)

