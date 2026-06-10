## Why

The solution has the full domain model, repository interfaces, empty service classes, DTO placeholders with `NotImplementedException` mappers, and empty DbContext shells. No layer is wired together yet — contexts have no DbSets or relationship configuration, mappers do nothing, services are empty, and the Web layer only registers Identity. Until these gaps are filled, the application cannot persist data, run migrations, or serve any business feature.

## What Changes

- Configure `AppDbContext` with DbSets for all business entities, relationship mappings, foreign keys, indexes, delete behaviors, and user-ownership filtering.
- Configure `AppIdentityDbContext` for Identity with an `AppUser`-based setup, kept separate from the business context.
- Implement all DataAccess mapper classes (`DTO/DataAccess/Mapper`) to map between Domain entities and DataAccess DTO entities.
- Implement all Web mapper classes (`DTO/Web/Mapper`) to map between Domain entities and Web DTOs.
- Implement the five Application services (`CreateAddressService`, `CreateContactService`, `CreateInvoiceService`, `GenerateMonthlyStatementService`, `SendMonthlyStatementService`) with business logic using repository CRUD methods.
- Define service interface methods in `Contracts/Application` to match the service implementations.
- Wire up DI registration in `Web/Program.cs` for contexts, repositories, mappers, and application services.
- Switch the database provider from SQLite to PostgreSQL (`Npgsql.EntityFrameworkCore.PostgreSQL`).
- Create EF Core migrations for both `AppDbContext` and `AppIdentityDbContext` with separate migration histories.
- Scaffold MVC CRUD pages for all entities, then adapt the monthly statement views to show statement details, contacts, calculated amounts, and a Send button.
- Add ViewModels where views need more than a single entity.
- Enforce user-ownership scoping in all reads, writes, updates, and deletes.

## Capabilities

### New Capabilities
- `db-contexts`: Configure AppDbContext and AppIdentityDbContext with all entity relationships, ownership filtering, and migration readiness.
- `entity-mappers`: Implement explicit Domain ↔ DataAccess DTO and Domain ↔ Web DTO mapper classes.
- `application-services`: Implement the five application services with business rules (equal split, monthly aggregation, send email per contact).
- `identity-ownership`: Enforce per-user data scoping across repositories, services, and controllers.
- `ef-migrations`: Create and maintain separate EF Core migration histories for both contexts against PostgreSQL.
- `web-scaffolding`: Scaffold MVC CRUD pages, add ViewModels, adapt monthly statement UI with statement block, contacts, amounts, and Send button.
- `di-wiring`: Register all contexts, repositories, mappers, and services in the DI container.

### Modified Capabilities
<!-- No existing specs to modify -->

## Impact

- **DataAccess/Context**: `AppDbContext.cs` and `AppIdentityDbContext.cs` gain full configuration.
- **DTO/DataAccess/Mapper**: All 8 mapper classes replace `NotImplementedException` with working code.
- **DTO/Web/Mapper**: All 8 mapper classes replace `NotImplementedException` with working code.
- **Contracts/Application**: All 5 service interfaces gain method signatures.
- **Application**: All 5 service classes gain implementations.
- **Web/Program.cs**: DI registration for contexts, repos, mappers, services; PostgreSQL provider swap.
- **Web/Controllers**: New or scaffolded controllers for each entity.
- **Web/Views**: Scaffolded CRUD views plus custom monthly statement views.
- **Web/Models**: New ViewModel classes for composite views.
- **Web.csproj / DataAccess.csproj**: New NuGet dependency on `Npgsql.EntityFrameworkCore.PostgreSQL`.
- **Migrations**: New migration folders for both contexts.
- **appsettings.json**: PostgreSQL connection string.

