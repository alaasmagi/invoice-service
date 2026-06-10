## Context

The invoice-service solution has a fully defined domain model (9 entities, 2 enums), repository interfaces and implementations (via `alaasmagi.Base.DataAccess.EF`), DataAccess DTO entities (with `BaseEntityUserWithMetaConcurrency`), Web DTOs (with `BaseEntityUserWithConcurrency`), and mapper class stubs that all throw `NotImplementedException`. The two DbContext shells (`AppDbContext`, `AppIdentityDbContext`) exist but contain no configuration. Application service classes exist but are empty. The Web layer has Identity wired to `AppIdentityDbContext` with SQLite but no business controllers or views. The goal is to fill all these gaps so the application can persist data, run business logic, and render MVC pages — without changing the existing architecture.

## Goals / Non-Goals

**Goals:**
- Configure `AppDbContext` with DbSets for all DataAccess DTO entities, explicit relationships, FK indexes, delete behaviors, and user-ownership column (`AppUserId`).
- Configure `AppIdentityDbContext` with `AppUser` extending `IdentityUser`, kept completely separate from the business context.
- Implement all 16 mapper classes (8 DataAccess + 8 Web) with explicit property-by-property mapping.
- Define method signatures on the 5 `Contracts/Application` service interfaces.
- Implement the 5 Application services using repository CRUD operations and business rules.
- Register all contexts, repositories, mappers, and services in `Program.cs`.
- Switch from SQLite to PostgreSQL (`Npgsql.EntityFrameworkCore.PostgreSQL`).
- Generate EF Core migrations separately for each context.
- Scaffold MVC CRUD controllers/views, then adapt monthly statement pages for the domain workflow.
- Add ViewModels for views that need composite data (e.g., monthly statement details).

**Non-Goals:**
- Changing the existing domain model, naming, or project structure.
- Adding audit/metadata fields to Domain entities (those stay in DataAccess DTOs only).
- Implementing a real email sending provider (a placeholder/interface is sufficient).
- Adding unit or integration tests (separate effort).
- Multi-tenancy beyond single-user ownership scoping.
- API endpoints (MVC only).

## Decisions

### 1. EF Core maps DataAccess DTO entities, not Domain entities

**Decision**: `AppDbContext` registers DbSets of `AddressEntity`, `InvoiceEntity`, etc. from `DTO.DataAccess.DataAccess.DTO`. The Domain classes are never mapped to tables.

**Rationale**: The DataAccess DTOs already extend `BaseEntityUserWithMetaConcurrency` which carries `AppUserId`, `CreatedAt`, `UpdatedAt`, and `ConcurrencyStamp`. Domain entities intentionally omit these fields. The base repository (`BaseRepository<TDomain, TEntity, TMapper>`) already expects a DTO entity type for persistence and a mapper to convert between Domain and DTO. This keeps the Domain layer pure.

**Alternative considered**: Map Domain entities directly and add shadow properties for metadata. Rejected because it would blur the existing Domain/DTO separation and conflict with the base library design.

### 2. Separate connection strings for the two contexts

**Decision**: Use two PostgreSQL connection strings (`DefaultConnection` for `AppIdentityDbContext`, `AppConnection` for `AppDbContext`). Both may point to the same database but use separate migration history tables (`__EFMigrationsHistory_Identity` and `__EFMigrationsHistory_App`).

**Rationale**: Keeps migration histories cleanly separated. The two contexts can be hosted in the same database (simpler DevOps) or split later without code changes.

**Alternative considered**: Two separate databases. Adds operational complexity without clear benefit at this stage.

### 3. Ownership scoping via AppUserId column and global query filter

**Decision**: Every DataAccess DTO entity inherits `AppUserId` from its base class. `AppDbContext.OnModelCreating` applies a global query filter (`.HasQueryFilter(e => e.AppUserId == _currentUserId)`) on each entity so all reads are automatically scoped. Write operations set `AppUserId` from the authenticated user's claim.

**Rationale**: Global query filters prevent accidental data leaks. The `AppUserId` is a `string` matching `IdentityUser.Id`. Controllers extract it from `User.FindFirstValue(ClaimTypes.NameIdentifier)` and pass it to the context or service layer.

**Alternative considered**: Manual `Where` clauses in every repository. Error-prone, violates DRY.

### 4. Delete behavior strategy

**Decision**:
- `Address → AddressContacts`: Cascade delete
- `Address → Invoices`: Cascade delete
- `Address → MonthlyStatements`: Cascade delete
- `Contact → AddressContacts`: Cascade delete
- `Contact → InvoiceAllocations`: Restrict (prevent deleting a contact with allocations)
- `Contact → ContactMonthlyStatements`: Restrict
- `Invoice → InvoiceAllocations`: Cascade delete
- `MonthlyStatement → ContactMonthlyStatements`: Cascade delete
- `MonthlyStatement → Invoices`: No action (invoices exist independently)
- `Service → Invoices`: Restrict

**Rationale**: Cascade where a child has no independent meaning. Restrict where deletion would lose important financial records.

### 5. Invoice allocation: equal split by default

**Decision**: When `CreateInvoiceService` creates an invoice, it finds all active `AddressContact` records for that address on the invoice date. It creates one `InvoiceAllocation` per active contact with `AllocatedSum = TotalSum / activeContactCount`, rounding the remainder to the last contact.

**Rationale**: Matches the stated business rule. The `EAllocationMethod` enum exists for future extension (Percentage, Fixed) but the default path is Equal.

### 6. Monthly statement generation

**Decision**: `GenerateMonthlyStatementService` accepts an `addressId` and a `year/month`. It queries all invoices for that address in that month, sums them, creates or updates a `MonthlyStatement`, then creates one `ContactMonthlyStatement` per distinct contact found in the invoice allocations with each contact's summed amount.

**Rationale**: Aggregation at the service layer using repository queries keeps the logic testable and separated from persistence.

### 7. Email sending as an abstraction

**Decision**: `SendMonthlyStatementService` calls an `IEmailSender` interface (to be defined in Contracts) for each `ContactMonthlyStatement`. The initial implementation logs instead of sending. The service updates `EmailSent`, `EmailSentAt`, and the statement's `Status`.

**Rationale**: No email provider requirement yet. The abstraction lets a real provider be plugged in later.

### 8. PostgreSQL provider swap

**Decision**: Replace `Microsoft.EntityFrameworkCore.Sqlite` with `Npgsql.EntityFrameworkCore.PostgreSQL` in `Web.csproj` and `DataAccess.csproj`. Update connection strings in `appsettings.json`.

**Rationale**: PostgreSQL is the stated target. SQLite was the initial scaffolding default.

### 9. ViewModels for monthly statement pages

**Decision**: Create `MonthlyStatementDetailViewModel` containing the statement, its invoices, the contact breakdowns, and a flag indicating whether the Send button should be enabled. Use this in the Details/Send views rather than passing the domain entity directly.

**Rationale**: The monthly statement detail page needs data from multiple related entities. ViewModels avoid `ViewBag`/`ViewData`.

## Risks / Trade-offs

- **[Global query filter complexity]** → EF Core global query filters require the context to know the current user ID at construction time. Mitigation: inject `IHttpContextAccessor` into `AppDbContext` and extract the user claim. Ensure background tasks provide an explicit user context.
- **[Rounding in equal split]** → Splitting a decimal amount equally can leave remainder cents. Mitigation: assign the remainder to the last contact in the allocation list.
- **[No real email provider]** → Send action is a stub. Mitigation: `IEmailSender` interface makes it easy to swap in SendGrid/SMTP later.
- **[Migration ordering]** → Two contexts sharing one database may have ordering issues on initial `dotnet ef database update`. Mitigation: run Identity migrations first (creates user tables), then App migrations.
- **[Scaffolding churn]** → ASP.NET code generator produces boilerplate that needs manual adaptation. Mitigation: scaffold once, then maintain views by hand. Document which views are customized.

