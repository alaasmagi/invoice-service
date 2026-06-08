## ADDED Requirements

### Requirement: CreateAddressService creates an address for the authenticated user
`CreateAddressService` SHALL accept the address data and the authenticated user's ID, create an `Address` domain entity, set the user ownership, persist it via the address repository, and return the created entity.

#### Scenario: Create address with valid data
- **WHEN** `CreateAddressService` is called with a name, full address, and user ID
- **THEN** it SHALL persist an `Address` with the given properties and `AppUserId` set to the authenticated user
- **THEN** it SHALL return the created `Address` with a generated `Id`

### Requirement: CreateContactService creates a contact for the authenticated user
`CreateContactService` SHALL accept contact data and the authenticated user's ID, create a `Contact`, persist it via the contact repository, and return the created entity.

#### Scenario: Create contact with valid data
- **WHEN** `CreateContactService` is called with full name, email, optional phone, and user ID
- **THEN** it SHALL persist a `Contact` with the given properties and `AppUserId` set to the authenticated user

### Requirement: CreateInvoiceService creates an invoice with equal-split allocations
`CreateInvoiceService` SHALL accept invoice data (service ID, address ID, invoice date, period, total sum) and the authenticated user's ID. It SHALL create the `Invoice`, find all active `AddressContact` records for the address on the invoice date, and create one `InvoiceAllocation` per active contact with `AllocatedSum = TotalSum / activeContactCount`.

#### Scenario: Invoice with two active contacts splits equally
- **WHEN** an invoice for 100.00 is created at an address with 2 active contacts on the invoice date
- **THEN** 2 `InvoiceAllocation` records SHALL be created, each with `AllocatedSum` = 50.00

#### Scenario: Invoice with three contacts handles remainder
- **WHEN** an invoice for 100.00 is created at an address with 3 active contacts
- **THEN** 3 `InvoiceAllocation` records SHALL be created with amounts summing to exactly 100.00
- **THEN** the rounding remainder SHALL be assigned to the last allocation

#### Scenario: Invoice at address with no active contacts
- **WHEN** an invoice is created at an address with 0 active contacts on the invoice date
- **THEN** the invoice SHALL be created with no allocations

### Requirement: GenerateMonthlyStatementService aggregates invoices by address and month
`GenerateMonthlyStatementService` SHALL accept an address ID, year, month, and user ID. It SHALL query all invoices for that address in that month, sum their totals, create or update a `MonthlyStatement`, and create one `ContactMonthlyStatement` per distinct contact from the invoice allocations with each contact's summed allocation amount.

#### Scenario: Generate statement for address with invoices
- **WHEN** an address has 3 invoices in January 2026 totaling 300.00, allocated to 2 contacts (contact A: 150.00, contact B: 150.00)
- **THEN** a `MonthlyStatement` SHALL be created with `TotalSum` = 300.00, `Year` = 2026, `Month` = 1
- **THEN** 2 `ContactMonthlyStatement` records SHALL be created with the correct per-contact amounts

#### Scenario: Generate statement for month with no invoices
- **WHEN** an address has no invoices for the requested month
- **THEN** the service SHALL NOT create a `MonthlyStatement`

#### Scenario: Regenerate existing statement
- **WHEN** a `MonthlyStatement` already exists for the same address, year, and month
- **THEN** the service SHALL update the existing statement and recalculate contact amounts

### Requirement: SendMonthlyStatementService sends emails to each contact
`SendMonthlyStatementService` SHALL accept a monthly statement ID and user ID. It SHALL load the statement and its `ContactMonthlyStatement` records, call `IEmailSender` for each contact, update `EmailSent` and `EmailSentAt`, and update the statement's `Status` to `Sent`.

#### Scenario: Send statement with all contacts
- **WHEN** a monthly statement with 2 contacts is sent
- **THEN** `IEmailSender` SHALL be called once per contact with their email and amount
- **THEN** each `ContactMonthlyStatement.EmailSent` SHALL be set to `true`
- **THEN** the `MonthlyStatement.Status` SHALL be updated to `Sent`
- **THEN** `MonthlyStatement.SentAt` SHALL be set to the current UTC time

#### Scenario: Partial send failure
- **WHEN** sending fails for one contact but succeeds for another
- **THEN** the successful contact's `EmailSent` SHALL be `true`
- **THEN** the failed contact's `EmailSent` SHALL remain `false`
- **THEN** the `MonthlyStatement.Status` SHALL be set to `PartiallySent`

### Requirement: Application services use repository CRUD methods
All application services SHALL use the repository interfaces from `Contracts/DataAccess` for persistence operations. They SHALL NOT access `DbContext` directly unless absolutely required for a specific technical query.

#### Scenario: Service persists via repository
- **WHEN** any application service creates or queries an entity
- **THEN** it SHALL call the corresponding repository method (e.g., `IAddressRepository.Add`, `IInvoiceRepository.GetAll`)

