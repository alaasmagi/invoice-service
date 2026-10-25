## Why

Every invoice is split equally between the residents of its address on the invoice date. That rule is hardcoded in `GenerateMonthlyStatementService.SplitEvenly`. Real households often split costs differently: one person pays 60% of electricity, or someone pays a fixed amount for internet while the others share the rest. Today there is no way to express that short of editing statements by hand.

The model was already heading this way but never got there. `EAllocationMethod` (`Equal`, `Percentage`, `Fixed`) is defined but unused, and `InvoiceAllocation` has full CRUD UI, but statement generation ignores both.

## What Changes

- Add a configurable **allocation rule** per address, optionally narrowed to one service and bounded by a validity date range. The rule picks an `EAllocationMethod` and lists per-contact share values: percentages, or fixed amounts.
- The rule is structured data, not a free-text formula. Every rule can be validated when it is saved and explained on the statement.
- Statement generation picks the split for each invoice by precedence:
  1. manual `InvoiceAllocation` rows for that invoice
  2. a rule for the address and service
  3. the address default rule
  4. an equal split (today's behaviour)
- Split calculation moves out of `GenerateMonthlyStatementService` into small, testable Application strategies, one per method, with one shared rounding helper that never loses a cent.
- Each `MonthlyStatementLine` records how its amount was calculated (method, rule reference, share value), so regenerating after a rule changes stays explainable.
- `IGenerateMonthlyStatementService.GenerateAsync` also returns warnings for invoices it could not allocate (no residents, a manual override that doesn't add up, fixed amounts above the invoice total), so the UI can show them instead of just logging them to the console. **BREAKING** (internal contract only): the return type changes from `IReadOnlyList<MonthlyStatement>` to a result object.
- Full layered support, following the existing pattern:
  - Domain entities
  - DataAccess entities and EF configuration
  - entity mappers and Web DTO mappers
  - `WebDtoMapper` overloads
  - repository contracts and implementations
  - DI registration
  - an AppDbContext migration
- New **Split rules** UI (list, details with a live split preview, create/edit with an editable shares table, delete with consequences), built from the existing design-system components (`page-header`, `card`, `data-table`, `form-row`/`field`, `segmented`, `badge`, `alert`, `_DeleteNotice`) and fully localized (English keys plus the Estonian resx).
- Existing screens tell the truth about splitting:
  - Address details lists the address's rules.
  - Statement details and invoice details show the split used (`÷ 3`, `40 %`, `Fixed`, `Manual`) instead of always `÷ N`.
  - Residents copy no longer claims every invoice is split equally.
  - The existing **Allocations** screen is relabeled **Manual splits** to make its role as a per-invoice override clear.
- The email wire contract does not change: `fractions` still carries the number of people sharing the invoice.

## Capabilities

### New Capabilities
- `invoice-allocation-rules`: Configurable allocation rules (persistence, validation, rule resolution and precedence, the split strategies for each method, rounding) plus the Split rules management UI.

### Modified Capabilities
- `person-monthly-statements`:
  - "Address invoices are split evenly among active residents" becomes "split according to the resolved allocation method", with equal split as the fallback.
  - Statement lines must record how each amount was calculated.
  - Generation must report invoices it could not allocate.

## Impact

- **Domain:** new `AllocationRule` and `AllocationRuleShare`. `EAllocationMethod` keeps its values (append-only). `MonthlyStatementLine` gets `AllocationMethod`, `AllocationRuleId?` and `ShareValue?`.
- **DTO:** new `AllocationRuleEntity` and `AllocationRuleShareEntity` with their entity mappers, new `AllocationRuleDto` and `AllocationRuleShareDto` with their Web mappers, and updated `MonthlyStatementLine*` DTOs and mappers.
- **Contracts:**
  - new `IAllocationRuleRepository`
  - new `IInvoiceAllocationRepository` read method
  - new `IAllocationRuleValidator`
  - new `IInvoiceSplitCalculator`
  - changed `IGenerateMonthlyStatementService` return type
- **DataAccess:** two new DbSets, EF configuration (indexes, precision, cascade and restrict rules), a new repository, and an `AddAllocationRules` migration.
  - The migration must be applied before the `[DEPLOY]` commit, because migrations are not applied automatically.
- **Application:** split strategies, the rule resolver, the validator, and a refactored `GenerateMonthlyStatementService`.
- **Web:**
  - a new `AllocationRulesController` with views and ViewModels
  - `WebDtoMapper` overloads
  - nav changes
  - updated Addresses, MonthlyStatements, Invoices and AddressContacts views
  - `MonthlyStatementsController` shows generation warnings
  - new resx strings
- **Tests:** unit tests for the strategies, the resolver, rounding and validation, plus generation scenarios. `WireComplianceTests` must keep passing unchanged.
