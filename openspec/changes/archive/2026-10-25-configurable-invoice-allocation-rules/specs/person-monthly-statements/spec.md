## ADDED Requirements

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

## MODIFIED Requirements

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

## REMOVED Requirements

### Requirement: Address invoices are split evenly among active residents
**Reason**: Equal splitting is no longer the only behaviour. Allocation is now resolved per invoice from manual allocations or configured rules, with equal split as the fallback.
**Migration**: Covered by "Address invoices are split according to the resolved allocation method", whose equal-split scenarios keep the previous behaviour for addresses without rules. The "No active residents" scenario moves to "Generation reports invoices it could not allocate".
