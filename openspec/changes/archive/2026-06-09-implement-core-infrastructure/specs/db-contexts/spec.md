## ADDED Requirements

### Requirement: AppDbContext registers all business entity DbSets
The `AppDbContext` SHALL declare a `DbSet<T>` for each DataAccess DTO entity: `AddressEntity`, `AddressContactEntity`, `AppUserEntity`, `ContactEntity`, `ContactMonthlyStatementEntity`, `InvoiceEntity`, `InvoiceAllocationEntity`, `MonthlyStatementEntity`, `ServiceEntity`.

#### Scenario: All business entities are queryable
- **WHEN** the application starts and `AppDbContext` is resolved
- **THEN** every DataAccess DTO entity type SHALL be available as a `DbSet` on the context

### Requirement: AppDbContext configures explicit relationships
The `AppDbContext.OnModelCreating` SHALL configure all entity relationships with explicit foreign keys, navigation properties, and deliberate delete behaviors as defined in the design document.

#### Scenario: Address cascade deletes children
- **WHEN** an `AddressEntity` is deleted
- **THEN** its `AddressContactEntity`, `InvoiceEntity`, and `MonthlyStatementEntity` children SHALL be cascade deleted

#### Scenario: Contact restrict prevents deletion with allocations
- **WHEN** a `ContactEntity` has associated `InvoiceAllocationEntity` records
- **THEN** deleting the contact SHALL be prevented by a restrict constraint

#### Scenario: Invoice cascade deletes allocations
- **WHEN** an `InvoiceEntity` is deleted
- **THEN** its `InvoiceAllocationEntity` children SHALL be cascade deleted

#### Scenario: MonthlyStatement cascade deletes contact statements
- **WHEN** a `MonthlyStatementEntity` is deleted
- **THEN** its `ContactMonthlyStatementEntity` children SHALL be cascade deleted

#### Scenario: Service restrict prevents deletion with invoices
- **WHEN** a `ServiceEntity` has associated `InvoiceEntity` records
- **THEN** deleting the service SHALL be prevented by a restrict constraint

### Requirement: AppDbContext configures indexes
The `AppDbContext` SHALL create indexes on all foreign key columns and on frequently queried columns such as `AddressEntity.Name`, `InvoiceEntity.InvoiceDate`, `MonthlyStatementEntity.Year`/`Month`.

#### Scenario: Foreign key columns are indexed
- **WHEN** the database schema is generated from `AppDbContext`
- **THEN** every foreign key column SHALL have a corresponding index

### Requirement: AppIdentityDbContext extends IdentityDbContext
The `AppIdentityDbContext` SHALL extend `IdentityDbContext<AppUser>` where `AppUser` extends `IdentityUser` with a `Fullname` property. It SHALL remain separate from `AppDbContext`.

#### Scenario: Identity context resolves independently
- **WHEN** `AppIdentityDbContext` is resolved from DI
- **THEN** it SHALL provide Identity tables without any business entity DbSets

### Requirement: AppDbContext accepts constructor with DbContextOptions
The `AppDbContext` SHALL accept `DbContextOptions<AppDbContext>` via its constructor so it can be registered in DI and used by EF Core tooling.

#### Scenario: Context is registered in DI
- **WHEN** `builder.Services.AddDbContext<AppDbContext>(...)` is called
- **THEN** the context SHALL be resolvable and functional

