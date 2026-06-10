## Context

The current implementation treats a `MonthlyStatement` as an address/month aggregate and uses `ContactMonthlyStatement` as the per-contact amount beneath that address statement. The requested behavior changes the statement identity: a monthly statement is now one contact/person for one calendar month across every address where that person has an active relationship. Each statement must include all invoice shares owed by that person, including invoices from multiple addresses and service names for each invoice.

This is a cross-cutting business model change affecting domain entities, persistence DTOs, EF mapping/migrations, generation logic, send logic, and the MonthlyStatements UI. The key rule is that each invoice belongs to one address and one service; for a given invoice month, the invoice total is split evenly among all people actively related to that address for that invoice period/date, then each person's share is appended to that person's unified monthly statement.

## Goals / Non-Goals

**Goals:**
- Redefine monthly statements as contact/person + year/month records.
- Store statement line details that connect one person statement to one invoice, address, service, invoice total, resident count, and person share.
- Generate one statement per person per month across all active address relationships.
- Split each address invoice evenly among active residents for that address during the invoice period.
- Display statement details grouped or sortable by address and service in the Web UI.
- Send one email per person per month with the full address/service/invoice breakdown.
- Keep user ownership scoping on all generated statements and lines.

**Non-Goals:**
- Supporting custom split percentages or fixed custom allocations in this change.
- Supporting partial-month pro-rating by days lived at an address.
- Changing `Invoice` ownership, `AddressContact` history semantics, or `Service` semantics.
- Introducing payment processing.
- Reorganizing solution projects or replacing the repository/service architecture.

## Decisions

### 1. MonthlyStatement identity becomes contact + year + month

**Decision**: `MonthlyStatement` will reference `ContactId` instead of `AddressId` as its aggregate owner and will remain scoped by `UserId`. A unique logical statement is `(UserId, ContactId, Year, Month)`.

**Rationale**: The statement recipient is the person, not the address. This makes the statement total and email send status naturally belong to one recipient.

**Alternative considered**: Keep address/month statements and compose emails across multiple statements at send time. Rejected because the UI, status, and payment state would remain fragmented and would not represent the user's requested unified invoice.

### 2. Add explicit monthly statement line records

**Decision**: Add a line-level model, e.g. `MonthlyStatementLine`, for one invoice share on one person statement. Each line stores/links: `MonthlyStatementId`, `InvoiceId`, `AddressId`, `ServiceId`, invoice date/period, invoice total, active resident count, and allocated person amount.

**Rationale**: Statement lines are the auditable snapshot of how the person's total was formed. They support UI and email rendering without re-deriving details from mutable invoice/allocation state every time.

**Alternative considered**: Reuse `InvoiceAllocation` directly as statement lines. Rejected because allocations are invoice-level and do not carry statement send/payment lifecycle or snapshot details needed for a generated statement.

### 3. Split by active residents for each invoice address

**Decision**: For each invoice in the target month, find active `AddressContact` records for the invoice address. Split the invoice total equally among those active contacts and assign rounding remainder to the last deterministic contact by ID.

**Rationale**: This preserves the current equal-split default and extends it across multiple addresses. Deterministic ordering prevents inconsistent totals between runs.

**Alternative considered**: Use existing `InvoiceAllocation` records only. Rejected as the primary source because requested behavior is defined from address residency and invoices; existing allocations may be stale or absent. The implementation may still refresh `InvoiceAllocation` records from the same calculation if the app continues to expose them.

### 4. Generation replaces statement lines for the month

**Decision**: Regenerating statements for a month removes/replaces generated lines for affected person/month statements and recalculates totals/status. Sent statements should either be blocked from regeneration or reset to `ReadyToSend`; implementation should choose one explicit behavior and surface it in UI.

**Rationale**: Monthly totals must remain consistent with the current invoice/residency data. Silent partial updates are risky.

**Alternative considered**: Incrementally patch only changed lines. Rejected because it is more complex and easier to get wrong.

### 5. Email uses statement snapshot lines

**Decision**: `SendMonthlyStatementService` builds the email from the generated person/month statement and its persisted lines, not directly from live invoice queries.

**Rationale**: The recipient should receive exactly what the UI shows. This also keeps send behavior stable even if invoice data changes after generation.

**Alternative considered**: Query invoices during send. Rejected because it can diverge from the generated statement total.

## Risks / Trade-offs

- **[Breaking data model change]** -> Add a clear EF migration and decide whether old address/month statement data is discarded or regenerated.
- **[Historical statement accuracy]** -> Store line snapshots for address/service names or ensure referenced records cannot be deleted without preserving readable statement history.
- **[Rounding differences]** -> Use deterministic contact ordering and assign the remainder to one contact so line shares sum exactly to invoice total.
- **[Regeneration after sending]** -> Either block sent-statement regeneration or reset send status explicitly; do not silently mutate already-sent totals.
- **[Larger statement details]** -> Use line tables and indexed `(UserId, ContactId, Year, Month)` queries to avoid expensive recomputation in UI and email.

## Migration Plan

1. Add/modify domain and DTO models for contact-centered `MonthlyStatement` and `MonthlyStatementLine`.
2. Update EF mappings, indexes, delete behaviors, and migrations.
3. Update mappers and repositories for the new line model.
4. Rewrite generation logic to iterate invoices by month, split by active residents, and upsert person/month statements.
5. Rewrite UI models/views to show person name, addresses, services, invoices, shares, and totals.
6. Rewrite send logic and email payloads to use statement lines.
7. Decide how to handle existing address/month rows during migration: delete and regenerate, or migrate if enough data exists.

## Open Questions

- Should regeneration be blocked for statements already sent, or should it reset them to `ReadyToSend`?
- Should statement lines snapshot address/service names, or only reference address/service IDs and read names live?
- Should the generation screen generate statements for all people for a month, or allow filtering by person/address?
