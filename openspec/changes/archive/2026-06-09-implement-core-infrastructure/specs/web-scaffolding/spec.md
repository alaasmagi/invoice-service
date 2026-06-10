## ADDED Requirements

### Requirement: CRUD controllers for all business entities
The Web project SHALL have MVC controllers for Address, Contact, Service, Invoice, AddressContact, InvoiceAllocation, MonthlyStatement, and ContactMonthlyStatement with standard Index, Details, Create, Edit, Delete actions.

#### Scenario: Address CRUD pages exist
- **WHEN** an authenticated user navigates to `/Addresses`
- **THEN** they SHALL see an index of their addresses with links to Create, Edit, Details, and Delete

#### Scenario: Contact CRUD pages exist
- **WHEN** an authenticated user navigates to `/Contacts`
- **THEN** they SHALL see an index of their contacts with links to Create, Edit, Details, and Delete

### Requirement: Monthly statement detail page shows composite data
The monthly statement Details view SHALL display the statement block (address, period, total, status), the list of included invoices, the list of contacts with their calculated amounts, and a Send button when the statement is in `Draft` or `ReadyToSend` status.

#### Scenario: View monthly statement details
- **WHEN** an authenticated user opens a monthly statement's Details page
- **THEN** they SHALL see the address name, year/month period, total sum, and status
- **THEN** they SHALL see a table of invoices included in the statement
- **THEN** they SHALL see a table of contacts with their individual amounts and email-sent status

#### Scenario: Send button visible for Draft statement
- **WHEN** the monthly statement status is `Draft` or `ReadyToSend`
- **THEN** a Send button SHALL be visible on the Details page

#### Scenario: Send button hidden for Sent statement
- **WHEN** the monthly statement status is `Sent`
- **THEN** the Send button SHALL NOT be visible

### Requirement: Send action calls SendMonthlyStatementService
The monthly statement controller SHALL have a POST action for Send that calls `SendMonthlyStatementService`, then redirects back to the Details page.

#### Scenario: User clicks Send
- **WHEN** an authenticated user clicks the Send button on a monthly statement
- **THEN** the controller SHALL call `SendMonthlyStatementService` with the statement ID
- **THEN** the user SHALL be redirected to the updated Details page

### Requirement: ViewModels used for composite views
Views that require data from multiple entities SHALL use a ViewModel class from `Web/Models` instead of `ViewBag` or `ViewData`.

#### Scenario: Monthly statement detail uses ViewModel
- **WHEN** the monthly statement Details view is rendered
- **THEN** it SHALL receive a `MonthlyStatementDetailViewModel` containing the statement, invoices, and contact breakdowns

### Requirement: Controllers delegate to Application services
Web controllers SHALL call Application service methods for business operations (create invoice with allocations, generate statement, send statement) instead of performing persistence logic directly.

#### Scenario: Create invoice calls CreateInvoiceService
- **WHEN** a user submits the Create Invoice form
- **THEN** the controller SHALL call `CreateInvoiceService` which handles allocation creation

#### Scenario: Generate statement calls GenerateMonthlyStatementService
- **WHEN** a user triggers monthly statement generation
- **THEN** the controller SHALL call `GenerateMonthlyStatementService`

