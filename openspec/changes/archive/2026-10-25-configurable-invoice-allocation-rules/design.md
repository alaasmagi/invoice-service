## Context

Today `GenerateMonthlyStatementService` loads the month's invoices, finds the active `AddressContact` rows on each `InvoiceDate`, and calls a private `SplitEvenly`. The last contact absorbs the rounding difference. Two pieces of the model exist but nothing uses them:
- `EAllocationMethod` (`Equal`, `Percentage`, `Fixed`)
- `InvoiceAllocation` (per-invoice, per-contact `AllocatedSum`), which already has a CRUD UI (`InvoiceAllocationsController`, nav entry "Allocations")

The layering for every entity is:

```
Domain (Domain/*)                    pure entity, BaseEntityUser
  ↕ IMapper<Domain, Entity>          DTO/DataAccess/Mapper/*EntityMapper
DataAccess entity (DTO/DataAccess/DTO/*Entity, BaseEntityUserWithMetaConcurrency)
  ↕ EF config in AppDbContext, BaseRepository<Domain, Entity, IMapper<…>>

Domain
  ↕ IMapper<Dto, Domain>             DTO/Web/Mapper/*DtoMapper
Web DTO (DTO/Web/DTO/*Dto, BaseEntityUserWithConcurrency, [Display] only here)

WebDtoMapper (Web/Mapping)           Entity → Domain → Dto (and back), carries loaded navigations
```

CRUD controllers (`AddressContactsController`, …) query `AppDbContext` directly, use `WebDtoMapper` and the `UserScopedControllerBase` helpers (`StampNew`, `StampExisting`, `Flash`, `ClearServerManagedModelState`, `RedirectToLocalOr`), and render Razor views styled by `_DesignSystem.cshtml`. Application services use repositories.

## Goals / Non-Goals

**Goals:**
- Configurable split per address, with an optional per-service override and validity in time, covering the Equal, Percentage and Fixed methods.
- One calculation path used by both statement generation and the UI preview.
- Each statement line records how its amount was calculated.
- Addresses without rules behave exactly as today.
- New code follows the existing layering, mapping and UI patterns. No new architecture.

**Non-Goals:**
- Free-text formulas or expression evaluation.
- Splits based on usage (meter readings) or prorating by days lived there. Both fit the strategy design later as new methods (`Prorated`, `Metered`).
- Changing who counts as active: still judged on `InvoiceDate`.
- Changing the email-hub wire contract.
- A REST API for rules.

## Decisions

### D1. Structured rule entity instead of a formula string
`AllocationRule { AddressId, ServiceId?, Method, ValidFrom, ValidTo? }` plus `AllocationRuleShare { AllocationRuleId, ContactId, Value }`.
- **Alternatives considered:**
  - A free-text formula column, evaluated with a library like NCalc. Rejected: rules can't be validated when saved, money calculations are hard to explain, it adds a dependency, and the editing UI becomes a code editor.
  - Weights stored directly on `AddressContact`. Rejected: no per-service variation and no history independent of residency.
- **Why `ServiceId` is nullable:** one address default plus a few service exceptions covers the real cases with the fewest rows.

### D2. Precedence: Manual → service rule → address rule → Equal fallback
The resolver is a pure function: `(invoice, manualAllocations, rules, activeResidents) → SplitPlan`.
- **Why it reuses `InvoiceAllocation` as a manual override:** that entity already exists and has UI. Giving it a defined role is cheaper and clearer than deleting it.
- **`IsOverride` flag (decided during implementation):** `CreateInvoiceService` used to write equal allocation rows for every invoice added by hand. Treating those as overrides would freeze such invoices at an equal split forever. So only rows with `IsOverride` = true count; they are set by the Manual splits UI. Existing rows default to false and stay record-only, shown with a "Record only" badge. Invoice creation no longer writes allocation rows.
- **No overlaps:** save-time validation forbids overlapping rules in the same (address, service scope), so the resolver never has to break ties.

### D3. Strategy per method in Application, behind one calculator
```
Contracts/Application/IInvoiceSplitCalculator.cs
    InvoiceSplitResult Calculate(InvoiceSplitInput input)
    AllocationRule? ResolveRule(Invoice invoice, IEnumerable<AllocationRule> rules)   // also used by previews
Application/Allocation/
    InvoiceSplitCalculator.cs         // resolves precedence, dispatches to strategy
    IAllocationStrategy.cs            // EAllocationMethod Method { get; }  Split(...)
    EqualAllocationStrategy.cs
    PercentageAllocationStrategy.cs
    FixedAllocationStrategy.cs
    ManualAllocationStrategy.cs
    MoneySplitter.cs                  // shared largest-remainder rounding
```
- **Pure calculator:** `InvoiceSplitInput` carries the invoice, its manual allocations, the candidate rules (with shares) and the active residents. The calculator does no I/O, so it is unit-testable, and the rule details preview runs the same code.
- **Output:** `InvoiceSplitResult` contains:
  - `Shares`: a list of `(ContactId, Amount, ShareValue?)`
  - `Method`
  - `RuleId?`
  - `ParticipantCount`
  - `Warnings`: a list of `AllocationWarning`
- **Registration:** strategies are registered explicitly, not discovered by reflection, in line with the project's code style.
- **Rounding:** `MoneySplitter.Split(total, weights ordered by ContactId)` uses largest remainder, with contact id as the tie-break.
  - This replaces the "last contact absorbs the difference" approach. Equal splits keep the same totals, and for equal weights the extra cents still go deterministically to the earliest contacts by id.
  - Existing equal-split scenarios (100.00 / 3) still give 33.33, 33.33, 33.34 in some order. The spec only requires a deterministic order that adds up exactly.

### D4. Line snapshot fields
Add three fields to `MonthlyStatementLine`, its DataAccess entity, its Web DTO, both mappers and the EF configuration:
- `AllocationMethod` (int, default 0 = Equal)
- `AllocationRuleId?` (FK to `AllocationRules`, `OnDelete SetNull`)
- `ShareValue?` (decimal(18,2))

`ResidentCount` keeps its meaning as the number of participants, so `SendMonthlyStatementService` and the `fractions` wire field don't change.

### D5. Generation result instead of a list
`IGenerateMonthlyStatementService.GenerateAsync` returns `MonthlyStatementGenerationResult { IReadOnlyList<MonthlyStatement> Statements; IReadOnlyList<AllocationWarning> Warnings }`.
- **`AllocationWarning` shape:** a class with typed fields instead of a generic args list: `InvoiceId`, `Kind`, `ContactId?`/`ContactName?`, `Amount?`, `RuleId?`, plus `AddressId`, `AddressName`, `ServiceName` and `InvoiceDate`, which generation fills in. All new contract types are plain classes (no records, no `sealed`), in line with project convention.
- **Localization stays in Web:** Application emits codes, and Web maps each `Kind` to a resx key.
- **Warning kinds:** `NoActiveResidents`, `UnlistedResident`, `NoListedResidentActive`, `FixedExceedsTotal`, `RemainderWithoutPayer`, `ManualSumMismatch`.
- **Display:** `MonthlyStatementsController` serializes the warnings to TempData (JSON) and the Index view renders them in an `alert--warning` list, with links to the fix (Residents, Split rules, Manual splits).
- The only caller is `MonthlyStatementsController.Generate`, so the breaking change stays contained.

### D6. Repositories and service data access
- New `IAllocationRuleRepository : IBaseRepository<AllocationRule>` with `GetForUserWithSharesAsync(userId)`. The repository includes shares, because the base `GetAllAsync` doesn't load child collections.
- `IInvoiceAllocationRepository` gets `GetByInvoiceIdsAsync(ids, userId)`.
- `GenerateMonthlyStatementService` loads rules and manual allocations once per run, groups them in memory, and calls `IInvoiceSplitCalculator` for each invoice. No per-invoice queries.

### D7. Validation lives in Application, is used by the controller
`IAllocationRuleValidator.ValidateAsync(AllocationRule rule, Guid userId) → IReadOnlyList<AllocationRuleError { string? Field, EAllocationRuleErrorKind Kind, string[] Args }>`.
- The validator uses repositories, and the controller maps each error to `ModelState`, including keys like `Shares[1].Value`.
- This keeps the business rules (percentages add up to 100, no overlaps, residency overlap) testable outside MVC.

### D8. UI composition (same design language)
- **Controller:** `AllocationRulesController : UserScopedControllerBase`, following the `AddressContactsController` pattern for querying, ownership checks, stamping, flashing and `returnUrl`.
- **ViewModels instead of ViewData** for the new pages, because a form with repeating share rows needs more than one DTO. They live in `Web/Models/AllocationRuleViewModels.cs`:
  - `AllocationRuleFormViewModel { AllocationRuleDto Rule; List<AllocationRuleShareDto> Shares; IReadOnlyList<SelectListItem> Addresses, Services, Contacts; string? ReturnUrl }`
  - `AllocationRuleIndexViewModel { rules, address filter, showPast, pastCount }`
  - `AllocationRuleDetailsViewModel { rule, preview: InvoiceSplitResult + invoice label or sample amount }`
- **Views:** `Web/Views/AllocationRules/{Index,Create,Edit,Details,Delete}.cshtml` plus a shared `_AllocationRuleFields.cshtml`. They reuse:
  - `page-header` (kicker "Directory")
  - `card form` (not `narrow`, because of the shares table)
  - `form-row`, `field` and `field__control` with the `i-chevron-down` and `i-calendar` icons
  - `segmented` with radio inputs for the method, so it works without JS
  - `data-table` inside the card for share rows, `icon-btn icon-btn--danger` to remove a row, and a `btn--ghost btn--sm` "Add person" button
  - `badge` tones for the method: neutral for Equal, accent for Percentage, warning for Fixed
  - `badge--success` "Ongoing"
  - `alert--warning` for preview warnings
  - `details__row` and `fact` for the details facts
  - `_DeleteNotice` for delete
- **Share rows:** index-based (`Rule.Shares[i].ContactId` / `Rule.Shares[i].Value`; the POST binds with `[Bind(Prefix = "Rule")]`). A `<template>` row is cloned by a `[data-rule-form]` block in `wwwroot/js/app.js`, next to the other page behaviours. The script also reindexes rows, updates the running total and value suffix, and hides the shares section for Equal. Blank rows are ignored on post. Without JS, the server renders the existing rows plus one empty row.
- **Resident prefill:** `GET AllocationRules/Residents?addressId=&date=` returns JSON `[{id, fullName}]` for residents active on that date. It is user-scoped and used when the address changes on a new rule. The server also prefills when `Create` gets an `addressId`.
- **Nav and wording:**
  - "Split rules" (`i-split`) goes in the Directory group after Residents.
  - The "Allocations" entry in Billing becomes "Manual splits" (`i-pencil`), with updated page copy.
  - Address details gets a "Split rules" section.
  - Statement and invoice details replace the `÷ N` cell with a `_SplitLabel` partial driven by `(AllocationMethod, ShareValue, ResidentCount)`.
- **Localization:** all new strings use English keys in `SharedResource.et.resx`. `LocalizationTests` must pass.

### D9. Persistence configuration
- `AllocationRuleEntity`:
  - indexes on `(UserId, AddressId, ServiceId, ValidFrom)`
  - FK to Address `Restrict` (deleting an address with rules is blocked, and the address delete page lists rules as blockers)
  - FK to Service `Restrict`
  - `HasMany(Shares)` with `Cascade`
- `AllocationRuleShareEntity`:
  - `Value` precision (18,2)
  - unique index `(AllocationRuleId, ContactId)`
  - FK to Contact `Restrict` (the contact delete page lists rules as blockers)
- `Method` is stored as int, matching how the existing enums are stored.

## Risks / Trade-offs

- [Statements already generated don't change when a rule changes] → Toast and delete consequences tell the user to regenerate, and the line snapshot fields make old statements explainable.
- [An unlisted new resident silently pays nothing under Percentage] → A generation warning names them, and the Address details page flags active residents missing from active rules.
- [The rounding change (largest remainder instead of last-absorbs) can move a cent between contacts on regeneration] → Totals are unchanged. This is accepted and noted in the change; tests pin the new behaviour.
- [Changing the `GenerateAsync` return type] → There is one in-repo caller, updated in the same change.
- [Restrict FKs block deleting addresses, services or contacts that are used by rules] → Delete pages list the rules as blockers through the existing `_DeleteNotice.Blockers` mechanism.

## Migration Plan

1. Add the entities, configuration and the `AddAllocationRules` migration to AppDbContext. The migration creates `AllocationRules` and `AllocationRuleShares`, and adds `AllocationMethod` (default 0), `AllocationRuleId` and `ShareValue` to `MonthlyStatementLines`.
2. Apply the migration to the production DB **before** pushing the `[DEPLOY]` commit, because deploy doesn't run migrations.
3. No data backfill is needed. Existing lines default to Equal with no rule, which is exactly how they were generated.
4. Rollback: redeploy the previous image. The new columns and tables are ignored by the old code, and the migration's `Down` drops them if needed.

## Open Questions

- Should Percentage also accept free weights (e.g. 2:1) and scale them, instead of requiring a total of exactly 100? The current proposal requires 100 for clarity. Weights could be added later as a `Weighted` method.
- Should active residents become period-aware (prorated) in a follow-up change? This is out of scope here.
