## 1. NuGet Dependencies

- [ ] 1.1 Add `Npgsql.EntityFrameworkCore.PostgreSQL` to `DataAccess.csproj` and `Web.csproj`; remove `Microsoft.EntityFrameworkCore.Sqlite` from `Web.csproj`
- [ ] 1.2 Add project reference from `Application.csproj` to `Contracts.csproj` and `Domain.csproj`
- [ ] 1.3 Update `appsettings.json` and `appsettings.Development.json` with PostgreSQL connection strings (`DefaultConnection` for Identity, `AppConnection` for business data)

## 2. DbContext Configuration

- [ ] 2.1 Configure `AppIdentityDbContext` to extend `IdentityDbContext<AppUser>` where `AppUser` extends `IdentityUser`; add constructor accepting `DbContextOptions<AppIdentityDbContext>`; configure separate migration history table `__EFMigrationsHistory_Identity`
- [ ] 2.2 Configure `AppDbContext` with constructor accepting `DbContextOptions<AppDbContext>` and `IHttpContextAccessor`; add DbSets for all 9 DataAccess DTO entities
- [ ] 2.3 In `AppDbContext.OnModelCreating`, configure all entity relationships, foreign keys, delete behaviors (cascade/restrict per design), and FK indexes
- [ ] 2.4 In `AppDbContext.OnModelCreating`, apply global query filters on `AppUserId` for ownership scoping; configure separate migration history table `__EFMigrationsHistory_App`
- [ ] 2.5 Override `SaveChangesAsync` in `AppDbContext` to auto-set `AppUserId`, `CreatedAt`, `UpdatedAt` on new/modified entities

## 3. DataAccess Mappers

- [ ] 3.1 Implement `AddressEntityMapper` — map `Address` ↔ `AddressEntity` with all scalar/FK properties
- [ ] 3.2 Implement `AddressContactEntityMapper` — map `AddressContact` ↔ `AddressContactEntity`
- [ ] 3.3 Implement `ContactEntityMapper` — map `Contact` ↔ `ContactEntity`
- [ ] 3.4 Implement `ContactMonthlyStatementEntityMapper` — map `ContactMonthlyStatement` ↔ `ContactMonthlyStatementEntity`
- [ ] 3.5 Implement `InvoiceEntityMapper` — map `Invoice` ↔ `InvoiceEntity`
- [ ] 3.6 Implement `InvoiceAllocationEntityMapper` — map `InvoiceAllocation` ↔ `InvoiceAllocationEntity`
- [ ] 3.7 Implement `MonthlyStatementEntityMapper` — map `MonthlyStatement` ↔ `MonthlyStatementEntity`
- [ ] 3.8 Implement `ServiceEntityMapper` — map `Service` ↔ `ServiceEntity`

## 4. Web Mappers

- [ ] 4.1 Implement `AddressDtoMapper` — map `Address` ↔ `AddressDto`
- [ ] 4.2 Implement `AddressContactDtoMapper` — map `AddressContact` ↔ `AddressContactDto`
- [ ] 4.3 Implement `ContactDtoMapper` — map `Contact` ↔ `ContactDto`
- [ ] 4.4 Implement `ContactMonthlyStatementDtoMapper` — map `ContactMonthlyStatement` ↔ `ContactMonthlyStatementDto`
- [ ] 4.5 Implement `InvoiceDtoMapper` — map `Invoice` ↔ `InvoiceDto`
- [ ] 4.6 Implement `InvoiceAllocationDtoMapper` — map `InvoiceAllocation` ↔ `InvoiceAllocationDto`
- [ ] 4.7 Implement `MonthlyStatementDtoMapper` — map `MonthlyStatement` ↔ `MonthlyStatementDto`
- [ ] 4.8 Implement `ServiceDtoMapper` — map `Service` ↔ `ServiceDto`

## 5. Contract Interfaces

- [ ] 5.1 Define `ICreateAddressService` methods: `Task<Address> CreateAsync(Address address, string userId)`
- [ ] 5.2 Define `ICreateContactService` methods: `Task<Contact> CreateAsync(Contact contact, string userId)`
- [ ] 5.3 Define `ICreateInvoiceService` methods: `Task<Invoice> CreateAsync(Invoice invoice, string userId)` (handles allocation creation internally)
- [ ] 5.4 Define `IGenerateMonthlyStatementService` methods: `Task<MonthlyStatement?> GenerateAsync(Guid addressId, int year, int month, string userId)`
- [ ] 5.5 Define `ISendMonthlyStatementService` methods: `Task SendAsync(Guid monthlyStatementId, string userId)`
- [ ] 5.6 Define `IEmailSender` interface in `Contracts/Application`: `Task SendMonthlyStatementEmailAsync(string toEmail, string contactName, decimal amount, string period)`

## 6. Application Services

- [ ] 6.1 Implement `CreateAddressService` — set `AppUserId`, call `IAddressRepository.Add`, save via UoW
- [ ] 6.2 Implement `CreateContactService` — set `AppUserId`, call `IContactRepository.Add`, save via UoW
- [ ] 6.3 Implement `CreateInvoiceService` — create invoice, find active `AddressContact` records for the invoice date, create equal-split `InvoiceAllocation` per contact, save via UoW
- [ ] 6.4 Implement `GenerateMonthlyStatementService` — query invoices for address/month, sum totals, create/update `MonthlyStatement`, create `ContactMonthlyStatement` per distinct contact with summed amounts
- [ ] 6.5 Implement `SendMonthlyStatementService` — load statement + contacts, call `IEmailSender` per contact, update `EmailSent`/`EmailSentAt`/`Status`, handle partial failures
- [ ] 6.6 Create `ConsoleEmailSender` stub in Application that implements `IEmailSender` by logging to console

## 7. DI Wiring

- [ ] 7.1 Register `AppDbContext` with PostgreSQL provider and `AppConnection` connection string in `Program.cs`
- [ ] 7.2 Switch `AppIdentityDbContext` from SQLite to PostgreSQL with `DefaultConnection` in `Program.cs`
- [ ] 7.3 Register `IHttpContextAccessor` in `Program.cs`
- [ ] 7.4 Register all 8 DataAccess mapper implementations against `IMapper<,>` as scoped services
- [ ] 7.5 Register all 8 repository implementations against their `I*Repository` interfaces as scoped services
- [ ] 7.6 Register `DataAccessUow` against its UoW interface as a scoped service
- [ ] 7.7 Register all 5 application services against their `I*Service` interfaces as scoped services
- [ ] 7.8 Register `ConsoleEmailSender` against `IEmailSender` as scoped

## 8. EF Core Migrations

- [ ] 8.1 Create initial migration for `AppIdentityDbContext` with output folder `Migrations/Identity`
- [ ] 8.2 Create initial migration for `AppDbContext` with output folder `Migrations/App`
- [ ] 8.3 Verify both migrations compile and produce the expected schema

## 9. Web Controllers and Views

- [ ] 9.1 Scaffold CRUD controllers and views for Address, Contact, Service entities using `dotnet aspnet-codegenerator`
- [ ] 9.2 Scaffold CRUD controllers and views for Invoice, AddressContact entities
- [ ] 9.3 Scaffold CRUD controllers and views for MonthlyStatement, ContactMonthlyStatement, InvoiceAllocation entities
- [ ] 9.4 Add `[Authorize]` attribute to all business controllers
- [ ] 9.5 Update all controller Create/Edit actions to set `AppUserId` from `User.FindFirstValue(ClaimTypes.NameIdentifier)`
- [ ] 9.6 Update all controller actions to scope queries to the authenticated user
- [ ] 9.7 Create `MonthlyStatementDetailViewModel` in `Web/Models` with statement, invoices, contacts, and send-button visibility
- [ ] 9.8 Adapt MonthlyStatement Details view to use `MonthlyStatementDetailViewModel` — show statement block, invoice table, contact amounts table, and Send button
- [ ] 9.9 Add POST Send action to MonthlyStatement controller that calls `SendMonthlyStatementService`
- [ ] 9.10 Update Invoice Create view/controller to call `CreateInvoiceService` instead of direct persistence
- [ ] 9.11 Add a Generate action to MonthlyStatement controller that calls `GenerateMonthlyStatementService`
- [ ] 9.12 Verify all views compile and render without errors

