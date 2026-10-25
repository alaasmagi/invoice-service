## 1. Domain

- [x] 1.1 Append `Manual = 3` to `Domain/EAllocationMethod.cs` and give the existing members explicit values (`Equal = 0`, `Percentage = 1`, `Fixed = 2`)
- [x] 1.2 Add `Domain/AllocationRule.cs` (`AddressId`, `Address`, `ServiceId?`, `Service?`, `Method`, `ValidFrom`, `ValidTo?`, `Shares`, plus `IsValidOn(DateOnly)` and `Overlaps(DateOnly, DateOnly?)` helpers)
- [x] 1.3 Add `Domain/AllocationRuleShare.cs` (`AllocationRuleId`, `AllocationRule`, `ContactId`, `Contact`, `Value`)
- [x] 1.4 Add `AllocationMethod`, `AllocationRuleId?` and `ShareValue?` to `Domain/MonthlyStatementLine.cs`
- [x] 1.5 Add `AllocationRules` navigation collections to `Address` (and `AllocationRuleShares` to `Contact`), matching the existing collection style

## 2. DataAccess DTOs, mappers and EF configuration

- [x] 2.1 Add `DTO/DataAccess/DTO/AllocationRuleEntity.cs` and `AllocationRuleShareEntity.cs` (`BaseEntityUserWithMetaConcurrency`, navigation properties to Address, Service, Contact and Shares)
- [x] 2.2 Add the snapshot fields to `MonthlyStatementLineEntity` and a nullable `AllocationRule` navigation
- [x] 2.3 Add `AllocationRuleEntityMapper` and `AllocationRuleShareEntityMapper` (`IMapper<Domain, Entity>`, explicit mapping, the rule mapper maps `Shares` through the share mapper)
- [x] 2.4 Update `MonthlyStatementLineEntityMapper` for the three new fields
- [x] 2.5 Register the `AllocationRules` and `AllocationRuleShares` DbSets and configure them in `AppDbContext`: indexes, `Value`/`ShareValue` precision (18,2), unique `(AllocationRuleId, ContactId)`, Restrict FKs to Address, Service and Contact, Cascade from rule to shares, and SetNull from line to rule
- [x] 2.6 Create the `AddAllocationRules` migration for AppDbContext only. Check that `Up` adds the tables and columns with `AllocationMethod` defaulting to 0, and that `Down` reverses them

## 3. Contracts and repositories

- [x] 3.1 Add `Contracts/DataAccess/IAllocationRuleRepository.cs` with `GetForUserWithSharesAsync(Guid userId)`
- [x] 3.2 Implement `DataAccess/AllocationRuleRepository.cs` on `BaseRepository`, including `Shares`
- [x] 3.3 Add `GetByInvoiceIdsAsync(IReadOnlyCollection<Guid>, Guid userId)` to `IInvoiceAllocationRepository` and implement it
- [x] 3.4 Add the Application contracts:
  - `IInvoiceSplitCalculator`, with the `InvoiceSplitInput` and `InvoiceSplitResult` models
  - `AllocationWarning` and `EAllocationWarningKind`
  - `IAllocationRuleValidator`, with `AllocationRuleError` and `EAllocationRuleErrorKind`
- [x] 3.5 Change `IGenerateMonthlyStatementService.GenerateAsync` to return `MonthlyStatementGenerationResult { Statements, Warnings }`

## 4. Application: split calculation

- [x] 4.1 Add `Application/Allocation/MoneySplitter.cs`: largest-remainder split to 2 decimals with contact-id tie-break, so the parts always add up to the input
- [x] 4.2 Add `IAllocationStrategy` and the `EqualAllocationStrategy`, `PercentageAllocationStrategy` (scale active listed shares, warn about unlisted residents, fall back to Equal when none are active) and `FixedAllocationStrategy` (fixed amounts plus equal remainder; warn and skip on excess or a remainder nobody can pay)
- [x] 4.3 Add `ManualAllocationStrategy` (use the `InvoiceAllocation` rows as they are; warn and skip on a sum mismatch)
- [x] 4.4 Add `InvoiceSplitCalculator`: precedence Manual → service rule → address rule → Equal fallback, choosing rules valid on `InvoiceDate`, and filling in `Method`, `RuleId`, `ParticipantCount` and `ShareValue` (effective % for Percentage, the amount for fixed payers, null otherwise)
- [x] 4.5 Add `AllocationRuleValidator` covering every check in the "Rules are validated on save" requirement (ownership, dates, per-method share rules, duplicates, residency overlap, scope overlap)
- [x] 4.6 Refactor `GenerateMonthlyStatementService`:
  - load rules (with shares) and manual allocations once per run
  - call `IInvoiceSplitCalculator` for each invoice
  - fill in the line snapshot fields
  - collect warnings, replacing the `Console.WriteLine`
  - delete `SplitEvenly`
  - return the new result type
- [x] 4.8 Add `InvoiceAllocation.IsOverride` (Domain, entity, DTOs, mappers, migration). Only override rows feed generation and previews; the Manual splits UI sets the flag; `CreateInvoiceService` stops writing allocation rows
- [x] 4.7 Register the strategies, the calculator, the validator and `IAllocationRuleRepository` explicitly in `Web/Configuration/InvoiceServiceApplication.cs`, next to the existing registrations

## 5. Web DTOs and mapping

- [x] 5.1 Add `DTO/Web/DTO/AllocationRuleDto.cs` (`[Display]` labels: "Address", "Applies to", "Split method", "Valid from", "Valid until"; `Shares` collection) and `AllocationRuleShareDto.cs` ("Person", "Share")
- [x] 5.2 Add `AllocationRuleDtoMapper` and `AllocationRuleShareDtoMapper` (`IMapper<Dto, Domain>`) and register them
- [x] 5.3 Add `AllocationMethod`, `AllocationRuleId` and `ShareValue` to `MonthlyStatementLineDto` and update `MonthlyStatementLineDtoMapper`
- [x] 5.4 Add `WebDtoMapper` constructor parameters plus `ToDto(AllocationRuleEntity)` (carrying Address, Service, and Shares with Contact) and `ToEntity(AllocationRuleDto)`. Also add `ToDomain(AllocationRuleDto)` for the validator

## 6. Web: Split rules UI

- [x] 6.1 Add `Web/Models/AllocationRuleViewModels.cs` (`AllocationRuleIndexViewModel`, `AllocationRuleFormViewModel`, `AllocationRuleDetailsViewModel`)
- [x] 6.2 Add `AllocationRulesController`:
  - Index, with address filter and show-past toggle
  - Details, with the split preview through `IInvoiceSplitCalculator` (latest matching invoice, otherwise a 100.00 sample)
  - Create and Edit, using the validator → `ModelState` mapping, ignoring blank share rows, and supporting `returnUrl`
  - Delete, with `_DeleteNotice` consequences describing what invoices fall back to
  - the `Residents` JSON endpoint
  - user scoping on every query
- [x] 6.3 Add `Views/AllocationRules/_AllocationRuleFields.cshtml`: address, service ("All services") and date fields; the `segmented` method radios with help text; the shares `data-table` with a `<template>` row, add and remove actions, a value suffix that follows the method, and a running total; plus a small inline script for reindexing, totals, hiding shares for Equal, and prefilling residents on address change
- [x] 6.4 Add `Index`, `Create`, `Edit`, `Details` and `Delete` views using the existing page-header, card, data-table, badge, alert and toast patterns, including the empty state copy
- [x] 6.5 Add the "Split rules" nav item (`i-split`) to the Directory group in `_Layout.cshtml`, and relabel "Allocations" to "Manual splits" (`i-pencil`), updating the copy in the `InvoiceAllocations` views
- [x] 6.6 Add a "Split rules" section with an "Add split rule" action to Address details, make the explanation text depend on whether rules exist, and flag active residents who are missing from an active Percentage or Fixed rule
- [x] 6.7 Add a `Views/Shared/_SplitLabel.cshtml` partial and use it in the `MonthlyStatements/Details` and `Invoices/Details` line tables in place of `÷ N`
- [x] 6.8 Update the Residents (Index, Create, `_ResidentFields`, Address details) copy that says invoices are always split equally
- [x] 6.9 List rules as delete blockers on the Address, Service and Contact delete pages
- [x] 6.10 Update `MonthlyStatementsController.Generate` to use the new result and pass the warnings through TempData. Render them in `MonthlyStatements/Index` as an `alert--warning` list with fix links
- [x] 6.11 Add every new UI string to `SharedResource.et.resx` with Estonian translations

## 7. Tests and verification

- [x] 7.1 Unit tests for `MoneySplitter` (exact totals, deterministic tie-break, zero and single-participant cases)
- [x] 7.2 Unit tests for each strategy, covering every spec scenario (60/40 of 82.10, a moved-out contact scaled up, an unlisted resident warning, fixed 10.00 plus remainder, fixed above the total, a manual sum mismatch)
- [x] 7.3 Unit tests for `InvoiceSplitCalculator` precedence and validity dates
- [x] 7.4 Unit tests for `AllocationRuleValidator` (percentages not adding up to 100, overlapping scope, a service rule overlapping the address default is allowed, a contact who never lived there, the Manual method rejected)
- [x] 7.5 Generation tests: no rules gives the same totals as before, a rule-driven split, line snapshot fields filled in, and warnings returned
- [x] 7.6 A mapper round-trip test for `WebDtoMapper.ToDto(AllocationRuleEntity)` with loaded navigations
- [x] 7.7 Run `dotnet build` with no new warnings, and run `dotnet test`, including the unchanged `WireComplianceTests` and `LocalizationTests`
- [ ] 7.8 Manually check in the running app: create rules from Address details, the running total and validation, the details preview, generating a month with warnings, the statement Split column, and dark mode and phone width for the new pages
- [ ] 7.9 Apply the `AddAllocationRules` migration to the production DB before the `[DEPLOY]` commit
