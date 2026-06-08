## ADDED Requirements

### Requirement: Separate migrations for AppDbContext
EF Core migrations for `AppDbContext` SHALL be stored in a dedicated folder (e.g., `DataAccess/Migrations/App`) and use a separate migration history table (`__EFMigrationsHistory_App`).

#### Scenario: Create initial App migration
- **WHEN** `dotnet ef migrations add Init --context AppDbContext` is run
- **THEN** a migration SHALL be generated that creates all business entity tables with correct columns, relationships, and indexes

### Requirement: Separate migrations for AppIdentityDbContext
EF Core migrations for `AppIdentityDbContext` SHALL be stored in a dedicated folder (e.g., `DataAccess/Migrations/Identity`) and use a separate migration history table (`__EFMigrationsHistory_Identity`).

#### Scenario: Create initial Identity migration
- **WHEN** `dotnet ef migrations add Init --context AppIdentityDbContext` is run
- **THEN** a migration SHALL be generated that creates Identity tables (AspNetUsers, AspNetRoles, etc.)

### Requirement: Migration histories do not overlap
The two contexts SHALL use different `MigrationsHistoryTable` names so their migration records do not interfere.

#### Scenario: Both contexts are migrated to the same database
- **WHEN** both `AppDbContext` and `AppIdentityDbContext` migrations are applied to the same PostgreSQL database
- **THEN** each context's migration history SHALL be tracked in its own table

### Requirement: PostgreSQL provider is used
Both contexts SHALL be configured to use `Npgsql.EntityFrameworkCore.PostgreSQL` as the database provider.

#### Scenario: Database provider is PostgreSQL
- **WHEN** the application starts
- **THEN** both contexts SHALL connect using the Npgsql PostgreSQL provider

