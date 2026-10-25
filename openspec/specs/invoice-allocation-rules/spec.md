# invoice-allocation-rules Specification

## Purpose
TBD - created by archiving change configurable-invoice-allocation-rules. Update Purpose after archive.
## Requirements
### Requirement: Allocation rules are user-scoped, address-bound configuration
The system SHALL persist `AllocationRule` records owned by the authenticated user. Each rule SHALL reference:
- one address
- optionally one service (no service means the rule applies to every service at that address)
- an `EAllocationMethod`
- a `ValidFrom` date and an optional `ValidTo` date

A rule SHALL own zero or more `AllocationRuleShare` records. Each share references one contact and carries a decimal `Value`. Queries and writes SHALL only ever touch rules, shares, addresses, services and contacts owned by the current user.

#### Scenario: Rule is stored for an address and service
- **WHEN** the user saves a Percentage rule for address "Tehnika" and service "Electricity", valid from 2026-11-01, with shares Mari 60 and Jaan 40
- **THEN** one `AllocationRule` and two `AllocationRuleShare` rows SHALL be persisted under the user's id

#### Scenario: Another user's rule is not reachable
- **WHEN** a user requests the details, edit or delete page of a rule id owned by a different user
- **THEN** the system SHALL respond with Not Found

#### Scenario: Deleting a rule removes its shares
- **WHEN** a rule with two shares is deleted
- **THEN** both shares SHALL be deleted with it
- **THEN** existing statement lines that referenced the rule SHALL keep their amounts and have their rule reference cleared

### Requirement: Allocation method values are stable
`EAllocationMethod` SHALL keep its existing members and numeric values: `Equal` = 0, `Percentage` = 1, `Fixed` = 2. It SHALL add `Manual` = 3, which marks amounts that come from per-invoice `InvoiceAllocation` overrides. New members SHALL only ever be appended. A rule SHALL NOT be saved with method `Manual`.

#### Scenario: Manual is rejected as a rule method
- **WHEN** a rule is submitted with method `Manual`
- **THEN** validation SHALL fail and the rule SHALL NOT be saved

### Requirement: Rules are validated on save
The system SHALL validate a rule before saving it and SHALL reject it with field-level or form-level messages when any of these checks fail:
- The address, the service (if given) and every share contact must exist and be owned by the user.
- `ValidTo` must not be earlier than `ValidFrom`.
- Method `Equal` must have no shares.
- Method `Percentage` must have at least one share. Every value must be greater than 0 and at most 100, and the values must add up to exactly 100.00.
- Method `Fixed` must have at least one share, and every value must be greater than 0.
- A contact must not appear more than once in the same rule.
- Every share contact must have a residency (`AddressContact`) at the rule's address that overlaps the rule's validity range.
- No other rule of the same user may have the same address and the same service scope (the same service, or both "all services") with an overlapping validity range.

#### Scenario: Percentages do not add up to 100
- **WHEN** a Percentage rule is submitted with shares 60 and 30
- **THEN** validation SHALL fail with a message stating the shares add up to 90 % instead of 100 %

#### Scenario: Overlapping rule for the same scope
- **WHEN** an ongoing "all services" rule exists for address "Tehnika" from 2026-01-01 and another "all services" rule for "Tehnika" is submitted valid from 2026-06-01
- **THEN** validation SHALL fail and identify the conflicting rule

#### Scenario: Service rule may overlap an address default
- **WHEN** an ongoing "all services" rule exists for address "Tehnika" and an "Internet" rule for "Tehnika" is submitted for an overlapping range
- **THEN** validation SHALL succeed

#### Scenario: Share contact never lives at the address
- **WHEN** a share references a contact with no residency at the rule's address during the rule's validity range
- **THEN** validation SHALL fail for that share row

### Requirement: Split method is resolved per invoice by precedence
For each invoice, statement generation SHALL decide how to split it in this order, using the invoice date as the reference date:
1. If one or more `InvoiceAllocation` rows exist for the invoice, use them (method `Manual`).
2. Otherwise, if a rule for the invoice's address and service is valid on the reference date, use it.
3. Otherwise, if an "all services" rule for the invoice's address is valid on the reference date, use it.
4. Otherwise, split equally among active residents (method `Equal`, no rule).

#### Scenario: Service rule beats address default
- **WHEN** address "Tehnika" has an "all services" Equal rule and an "Electricity" Percentage rule, both valid, and an Electricity invoice is generated
- **THEN** the Percentage rule SHALL be used for that invoice

#### Scenario: Rule outside its validity is ignored
- **WHEN** a rule is valid from 2026-11-01 and an invoice is dated 2026-10-15
- **THEN** that rule SHALL NOT be used for the invoice

#### Scenario: Manual allocations override rules
- **WHEN** an invoice has manual `InvoiceAllocation` rows and its address also has a valid rule
- **THEN** the manual rows SHALL be used and the rule ignored

#### Scenario: No rule configured
- **WHEN** an address has no rules and no manual allocations exist for its invoice
- **THEN** the invoice SHALL be split equally among active residents, exactly as before this change

### Requirement: Equal method splits among active residents
Under `Equal`, the invoice total SHALL be split evenly among the contacts active at the invoice address on the invoice date.

#### Scenario: Equal rule with three residents
- **WHEN** a 100.00 invoice is split by Equal among three active residents
- **THEN** the shares SHALL be 33.33, 33.33 and 33.34 in deterministic contact order, adding up to 100.00

### Requirement: Percentage method splits by configured shares
Under `Percentage`, only share contacts who are active residents on the invoice date take part. Each participant's share SHALL be proportional to their percentage. If some share contacts are inactive, the remaining percentages SHALL be scaled up so they add up to 100. Active residents not listed in the rule SHALL pay nothing, and generation SHALL report a warning for each of them. If no share contact is active, the invoice SHALL fall back to an equal split among active residents, and generation SHALL report a warning.

#### Scenario: Two listed residents split 60/40
- **WHEN** a 82.10 invoice is split by a Percentage rule with Mari 60 and Jaan 40, both active
- **THEN** Mari SHALL receive 49.26 and Jaan 32.84

#### Scenario: Listed contact has moved out
- **WHEN** a rule lists Mari 50, Jaan 25 and Kati 25, Kati is no longer active, and the invoice is 90.00
- **THEN** Mari SHALL receive 60.00 (effective 66.67 %) and Jaan 30.00 (effective 33.33 %)

#### Scenario: Unlisted resident has moved in
- **WHEN** a Percentage rule lists Mari 60 and Jaan 40 and Peeter is also an active resident
- **THEN** Peeter SHALL receive no share of the invoice
- **THEN** generation SHALL report a warning that Peeter is not part of the rule

### Requirement: Fixed method charges fixed amounts and splits the remainder
Under `Fixed`, each active share contact SHALL pay their configured amount. The remainder (invoice total minus the fixed amounts) SHALL be split equally among active residents not listed in the rule. If the fixed amounts exceed the invoice total, or a non-zero remainder is left with nobody to pay it, the invoice SHALL NOT be allocated and generation SHALL report a warning.

#### Scenario: Fixed internet share
- **WHEN** a 40.00 Internet invoice uses a Fixed rule where Jaan pays 10.00, and Mari and Kati are also active
- **THEN** Jaan SHALL receive 10.00, and Mari and Kati SHALL receive 15.00 each

#### Scenario: Fixed amounts exceed the invoice
- **WHEN** fixed amounts add up to 50.00 and the invoice total is 40.00
- **THEN** no statement lines SHALL be created for that invoice
- **THEN** generation SHALL report a warning naming the invoice

### Requirement: Manual allocations must add up to the invoice total
Only `InvoiceAllocation` rows with `IsOverride` = true SHALL count as manual splits. Rows are marked as overrides when they are saved through the Manual splits UI. Existing rows, and rows written automatically by earlier versions, default to false and SHALL be ignored by generation. Invoice creation SHALL NOT write allocation rows. When manual splits are used, each row's `AllocatedSum` SHALL become that contact's share as it is, whether or not the contact is a resident. If the rows don't add up exactly to the invoice total, the invoice SHALL NOT be allocated and generation SHALL report a warning.

#### Scenario: Record-only rows are ignored
- **WHEN** an invoice has only `InvoiceAllocation` rows with `IsOverride` = false
- **THEN** the invoice SHALL be split by rules or equally, as if no manual split existed

#### Scenario: Manual rows do not add up
- **WHEN** an invoice of 100.00 has manual rows of 60.00 and 30.00
- **THEN** no statement lines SHALL be created for that invoice
- **THEN** generation SHALL report a warning that 10.00 is unallocated

### Requirement: Rounding never loses or invents a cent
Every split method SHALL round shares to 2 decimals using a single shared rounding routine. The rounded shares SHALL add up exactly to the amount being split. Any leftover cents SHALL be assigned deterministically, ordered by contact id, so regenerating the same data gives identical amounts.

#### Scenario: Percentage split with rounding remainder
- **WHEN** 10.00 is split by Percentage 33.33 / 33.33 / 33.34
- **THEN** the three shares SHALL add up to exactly 10.00
- **THEN** repeating the calculation SHALL give the same per-contact amounts

### Requirement: Split rules management UI
The Web app SHALL provide a "Split rules" area, listed in the Directory navigation group, built from the existing design-system components and localized through `SharedResource` (English keys and Estonian translations). It SHALL include:
- **Index:** a `data-table` of the user's rules showing:
  - address
  - scope (service name, or an "All services" badge)
  - method badge
  - a shares summary (e.g. "Mari 60 % · Jaan 40 %")
  - validity, with an "Ongoing" badge when there is no end date
  - edit and delete actions

  It also has an address filter, a toggle to show past rules (like Residents), and an empty state that explains invoices are split equally until a rule exists.
- **Create / Edit:**
  - Address, Service ("All services" option), Valid from and Valid to fields.
  - A segmented control to choose Equal, Percentage or Fixed, with help text for each.
  - An editable shares table with a person select, a value input whose suffix follows the method (% or currency), add and remove row actions, and a running total. The shares table is hidden for Equal.
  - Choosing an address on a new rule prefills share rows with the residents active on Valid from.
  - Validation messages appear inline per row and in the summary.
- **Details:**
  - the rule's facts and its shares table
  - a split preview, computed by the same Application calculator used for generation, applied to the most recent matching invoice (or to a sample amount of 100.00 when there is none), with resulting per-person amounts and any warnings shown as `alert--warning`
- **Delete:** a confirmation built on `_DeleteNotice`. It states what the affected invoices will fall back to (another rule or an equal split) and that existing statements change only when regenerated.

After a create, edit or delete, a toast SHALL remind the user to regenerate statements for affected months.

#### Scenario: Create a percentage rule from address details
- **WHEN** the user clicks "Add split rule" on the "Tehnika" address details page
- **THEN** the create form SHALL open with "Tehnika" selected, share rows prefilled with its current residents, and a return link back to the address

#### Scenario: Running total feedback
- **WHEN** the user enters 60 and 30 under the Percentage method
- **THEN** the shares table footer SHALL show a total of 90 % in a warning tone
- **THEN** the server SHALL reject the submission if it is posted anyway

#### Scenario: Preview on details page
- **WHEN** the user opens a rule whose address has an 82.10 Electricity invoice within the rule's validity
- **THEN** the details page SHALL show each participant's computed amount for that invoice

### Requirement: Existing screens reflect configured splitting
- Address details SHALL list the address's split rules, with an "Add split rule" action, and its explanation text SHALL reflect whether rules exist.
- Monthly statement details and invoice details SHALL show, for each line, how the amount was calculated: `÷ N` for Equal, the effective percentage for Percentage, "Fixed" or "Remainder ÷ N" for Fixed, and "Manual" for manual allocations.
- Resident pages SHALL no longer claim that every invoice is split equally.
- The existing Allocations navigation entry and pages SHALL be relabeled "Manual splits" and explain that they override rules for a single invoice.

#### Scenario: Statement line shows percentage split
- **WHEN** a statement line was generated by a Percentage rule with an effective share of 60 %
- **THEN** the statement details "Split" column SHALL show "60 %" instead of "÷ 2"

### Requirement: Layered persistence follows the existing DTO and mapper pattern
Allocation rules SHALL follow the existing layering:
- Domain: `AllocationRule`, `AllocationRuleShare`
- DataAccess entities: `AllocationRuleEntity`, `AllocationRuleShareEntity`, based on `BaseEntityUserWithMetaConcurrency`
- Explicit entity mappers implementing `IMapper<Domain, Entity>`
- Web DTOs: `AllocationRuleDto`, `AllocationRuleShareDto`, based on `BaseEntityUserWithConcurrency` and carrying the `[Display]` metadata
- Explicit Web mappers implementing `IMapper<Dto, Domain>`
- `WebDtoMapper` `ToDto` and `ToEntity` overloads that carry loaded navigation properties
- A repository contract and implementation built on `BaseRepository`
- DI registration in `InvoiceServiceApplication`

UI metadata SHALL NOT be placed on Domain or DataAccess types.

#### Scenario: Round trip through the layers
- **WHEN** an `AllocationRuleEntity` with loaded Address, Service and Shares (with Contacts) is passed to `WebDtoMapper.ToDto`
- **THEN** the resulting `AllocationRuleDto` SHALL contain the same field values, the related address and service names, and one share DTO per share with its contact name

