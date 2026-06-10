## 1. Model and Persistence Shape

- [x] 1.1 Update `Domain/MonthlyStatement.cs` so a statement references `ContactId` instead of `AddressId` and represents one contact/year/month
- [x] 1.2 Add a domain model for monthly statement invoice lines with statement, invoice, address, service, resident count, invoice total, and allocated amount fields
- [x] 1.3 Add matching DataAccess DTO entity for monthly statement lines
- [x] 1.4 Add matching Web DTO/ViewModel shape for monthly statement line display where needed
- [x] 1.5 Add mapper implementations for monthly statement lines and update monthly statement mappers for `ContactId`
- [x] 1.6 Add repository interface and implementation for monthly statement lines if the repository pattern requires it

## 2. EF Core Mapping and Migration

- [x] 2.1 Add `DbSet` for monthly statement lines to `AppDbContext`
- [x] 2.2 Configure `MonthlyStatementEntity` relationship to `ContactEntity` and remove/replace address-only relationship assumptions
- [x] 2.3 Configure `MonthlyStatementLineEntity` relationships to monthly statement, invoice, address, and service with deliberate delete behavior
- [x] 2.4 Add indexes for `(UserId, ContactId, Year, Month)` and statement line lookup by statement/invoice/address/service
- [x] 2.5 Create an EF migration that converts the statement schema from address/month to contact/month and adds statement lines
- [x] 2.6 Decide and implement migration behavior for existing address/month statements: discard/regenerate or migrate when possible

## 3. Statement Generation Algorithm

- [x] 3.1 Update `IGenerateMonthlyStatementService` contract if generation should run for a whole month instead of one address/month
- [x] 3.2 Rewrite `GenerateMonthlyStatementService` to load all invoices for the requested month owned by the authenticated user
- [x] 3.3 For each invoice, find active `AddressContact` records for the invoice address and invoice period/date
- [x] 3.4 Split each invoice total evenly among active contacts with deterministic rounding remainder handling
- [x] 3.5 Create or refresh one `MonthlyStatement` per contact/year/month
- [x] 3.6 Replace statement lines for regenerated statements and calculate totals from line allocated amounts
- [x] 3.7 Handle invoices with no active residents by surfacing/logging allocation failures without creating invalid statement lines
- [x] 3.8 Define behavior for regenerating already sent statements and implement it consistently

## 4. Web UI Updates

- [x] 4.1 Update `MonthlyStatementsController` Generate actions to generate person/month statements rather than address/month statements
- [x] 4.2 Update MonthlyStatements Index to show contact/person name, period, total, status, and sent date
- [x] 4.3 Update MonthlyStatement Details ViewModel to include person, addresses represented, statement lines, totals, and send visibility
- [x] 4.4 Update MonthlyStatement Details view to show address, service, invoice date/period, invoice total, resident count, and person's share per line
- [x] 4.5 Remove or adapt Create/Edit fields that no longer make sense for person-centered generated statements
- [x] 4.6 Ensure all MonthlyStatements queries remain scoped to the authenticated user

## 5. Email and Send Flow

- [x] 5.1 Update `SendMonthlyStatementService` to load the persisted person/month statement lines instead of recomputing invoice details at send time
- [x] 5.2 Update `MonthlyStatementEmail` payload to include address and service names for every statement line
- [x] 5.3 Update `BrevoEmailSender` HTML template to render the full person/month address/service/invoice breakdown
- [x] 5.4 Update `ConsoleEmailSender` to print the unified person/month statement breakdown for local development
- [x] 5.5 Ensure Send sends exactly one email per person/month statement and updates statement status correctly

## 6. Verification

- [ ] 6.1 Run `dotnet build invoice-service.sln` and fix compile errors
- [ ] 6.2 Generate statements for a month where one person has invoices at multiple addresses and verify only one statement is created for that person
- [ ] 6.3 Generate statements for an address with multiple residents and verify invoice totals split evenly across person statements
- [ ] 6.4 Verify Monthly Statement Details displays person name, addresses, service names, invoice totals, and person shares
- [ ] 6.5 Send a statement in Console email mode and verify one email body contains the full monthly breakdown
- [ ] 6.6 If Brevo credentials are available, send a statement through Brevo and verify the HTML email renders the breakdown correctly
