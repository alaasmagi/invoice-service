## 1. NuGet Dependencies

- [x] 1.1 Add `Npgsql.EntityFrameworkCore.PostgreSQL` to `DataAccess.csproj` and `Web.csproj`; remove `Microsoft.EntityFrameworkCore.Sqlite` from `Web.csproj`
- [x] 1.2 Add project reference from `Application.csproj` to `Contracts.csproj` and `Domain.csproj`
- [x] 1.3 Update `appsettings.json` and `appsettings.Development.json` with PostgreSQL connection strings (`DefaultConnection` for Identity, `AppConnection` for business data)

## 2. DbContext Configuration

- [x] 2.1 Configure `AppIdentityDbContext` to extend `IdentityDbContext<AppUser>` where `AppUser` extends `IdentityUser`; add constructor accepting `DbContextOptions<AppIdentityDbContext>`; configure separate migration history table `__EFMigrationsHistory_Identity`
- [x] 2.2 Configure `AppDbContext` with constructor accepting `DbContextOptions<AppDbContext>` and `IHttpContextAccessor`; add DbSets for all 9 DataAccess DTO entities
- [x] 2.3 In `AppDbContext.OnModelCreating`, configure all entity relationships, foreign keys, delete behaviors (cascade/restrict per design), and FK indexes
- [x] 2.4 In `AppDbContext.OnModelCreating`, apply global query filters on `AppUserId` for ownership scoping; configure separate migration history table `__EFMigrationsHistory_App`
- [x] 2.5 Override `SaveChangesAsync` in `AppDbContext` to auto-set `AppUserId`, `CreatedAt`, `UpdatedAt` on new/modified entities

## 3. DataAccess Mappers

- [x] 3.1 Implement `AddressEntityMapper` — map `Address` ↔ `AddressEntity` with all scalar/FK properties
- [x] 3.2 Implement `AddressContactEntityMapper` — map `AddressContact` ↔ `AddressContactEntity`
- [x] 3.3 Implement `ContactEntityMapper` — map `Contact` ↔ `ContactEntity`
- [x] 3.4 Implement `ContactMonthlyStatementEntityMapper` — map `ContactMonthlyStatement` ↔ `ContactMonthlyStatementEntity`
- [x] 3.5 Implement `InvoiceEntityMapper` — map `Invoice` ↔ `InvoiceEntity`
- [x] 3.6 Implement `InvoiceAllocationEntityMapper` — map `InvoiceAllocation` ↔ `InvoiceAllocationEntity`
- [x] 3.7 Implement `MonthlyStatementEntityMapper` — map `MonthlyStatement` ↔ `MonthlyStatementEntity`
- [x] 3.8 Implement `ServiceEntityMapper` — map `Service` ↔ `ServiceEntity`

## 4. Web Mappers

- [x] 4.1 Implement `AddressDtoMapper` — map `Address` ↔ `AddressDto`
- [x] 4.2 Implement `AddressContactDtoMapper` — map `AddressContact` ↔ `AddressContactDto`
- [x] 4.3 Implement `ContactDtoMapper` — map `Contact` ↔ `ContactDto`
- [x] 4.4 Implement `ContactMonthlyStatementDtoMapper` — map `ContactMonthlyStatement` ↔ `ContactMonthlyStatementDto`
- [x] 4.5 Implement `InvoiceDtoMapper` — map `Invoice` ↔ `InvoiceDto`
- [x] 4.6 Implement `InvoiceAllocationDtoMapper` — map `InvoiceAllocation` ↔ `InvoiceAllocationDto`
- [x] 4.7 Implement `MonthlyStatementDtoMapper` — map `MonthlyStatement` ↔ `MonthlyStatementDto`
- [x] 4.8 Implement `ServiceDtoMapper` — map `Service` ↔ `ServiceDto`

## 5. Contract Interfaces

- [x] 5.1 Define `ICreateAddressService` methods: `Task<Address> CreateAsync(Address address, string userId)`
- [x] 5.2 Define `ICreateContactService` methods: `Task<Contact> CreateAsync(Contact contact, string userId)`
- [x] 5.3 Define `ICreateInvoiceService` methods: `Task<Invoice> CreateAsync(Invoice invoice, string userId)` (handles allocation creation internally)
- [x] 5.4 Define `IGenerateMonthlyStatementService` methods: `Task<MonthlyStatement?> GenerateAsync(Guid addressId, int year, int month, string userId)`
- [x] 5.5 Define `ISendMonthlyStatementService` methods: `Task SendAsync(Guid monthlyStatementId, string userId)`
- [x] 5.6 Define `IEmailSender` interface in `Contracts/Application`: `Task SendMonthlyStatementEmailAsync(string toEmail, string contactName, decimal amount, string period)`

## 6. Application Services

- [x] 6.1 Implement `CreateAddressService` — set `AppUserId`, call `IAddressRepository.Add`, save via UoW
- [x] 6.2 Implement `CreateContactService` — set `AppUserId`, call `IContactRepository.Add`, save via UoW
- [x] 6.3 Implement `CreateInvoiceService` — create invoice, find active `AddressContact` records for the invoice date, create equal-split `InvoiceAllocation` per contact, save via UoW
- [x] 6.4 Implement `GenerateMonthlyStatementService` — query invoices for address/month, sum totals, create/update `MonthlyStatement`, create `ContactMonthlyStatement` per distinct contact with summed amounts
- [x] 6.5 Implement `SendMonthlyStatementService` — load statement + contacts, call `IEmailSender` per contact, update `EmailSent`/`EmailSentAt`/`Status`, handle partial failures
- [x] 6.6 Create `ConsoleEmailSender` stub in Application that implements `IEmailSender` by logging to console

## 7. DI Wiring

- [x] 7.1 Register `AppDbContext` with PostgreSQL provider and `AppConnection` connection string in `Program.cs`
- [x] 7.2 Switch `AppIdentityDbContext` from SQLite to PostgreSQL with `DefaultConnection` in `Program.cs`
- [x] 7.3 Register `IHttpContextAccessor` in `Program.cs`
- [x] 7.4 Register all 8 DataAccess mapper implementations against `IMapper<,>` as scoped services
- [x] 7.5 Register all 8 repository implementations against their `I*Repository` interfaces as scoped services
- [x] 7.6 Register `DataAccessUow` against its UoW interface as a scoped service
- [x] 7.7 Register all 5 application services against their `I*Service` interfaces as scoped services
- [x] 7.8 Register `ConsoleEmailSender` against `IEmailSender` as scoped

## 8. EF Core Migrations

- [x] 8.1 Create initial migration for `AppIdentityDbContext` with output folder `Migrations/Identity`
- [x] 8.2 Create initial migration for `AppDbContext` with output folder `Migrations/App`
- [ ] 8.3 Verify both migrations compile and produce the expected schema

## 9. Web Controllers and Views

- [x] 9.1 Scaffold CRUD controllers and views for Address, Contact, Service entities using `dotnet aspnet-codegenerator`
- [x] 9.2 Scaffold CRUD controllers and views for Invoice, AddressContact entities
- [x] 9.3 Scaffold CRUD controllers and views for MonthlyStatement, ContactMonthlyStatement, InvoiceAllocation entities
- [x] 9.4 Add `[Authorize]` attribute to all business controllers
- [x] 9.5 Update all controller Create/Edit actions to set `AppUserId` from `User.FindFirstValue(ClaimTypes.NameIdentifier)`
- [x] 9.6 Update all controller actions to scope queries to the authenticated user
- [x] 9.7 Create `MonthlyStatementDetailViewModel` in `Web/Models` with statement, invoices, contacts, and send-button visibility
- [x] 9.8 Adapt MonthlyStatement Details view to use `MonthlyStatementDetailViewModel` — show statement block, invoice table, contact amounts table, and Send button
- [x] 9.9 Add POST Send action to MonthlyStatement controller that calls `SendMonthlyStatementService`
- [x] 9.10 Update Invoice Create view/controller to call `CreateInvoiceService` instead of direct persistence
- [x] 9.11 Add a Generate action to MonthlyStatement controller that calls `GenerateMonthlyStatementService`
- [ ] 9.12 Verify all views compile and render without errors
