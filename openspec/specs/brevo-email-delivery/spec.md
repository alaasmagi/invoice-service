## ADDED Requirements

### Requirement: Monthly statement emails are sent through Brevo
The application SHALL provide a Brevo-backed implementation of `IEmailSender` that sends monthly statement emails through Brevo's transactional email API.

#### Scenario: Send monthly statement email succeeds
- **WHEN** `IEmailSender.SendMonthlyStatementEmailAsync` is called with recipient email, contact name, amount, and period
- **THEN** the Brevo sender SHALL submit a transactional email request to Brevo
- **THEN** the request SHALL include the configured sender email and sender name
- **THEN** the request SHALL include the recipient email and contact name

### Requirement: Brevo sender uses configured credentials
The Brevo sender SHALL read its API key, sender email, and sender name from validated configuration and SHALL NOT hard-code provider credentials.

#### Scenario: Brevo sender is constructed
- **WHEN** dependency injection creates the Brevo email sender
- **THEN** it SHALL use the configured Brevo API key for outbound requests
- **THEN** it SHALL use the configured sender identity for email payloads

### Requirement: Brevo failures surface to the application service
The Brevo sender SHALL treat non-success Brevo API responses as send failures and SHALL expose enough error detail for application logging or status handling.

#### Scenario: Brevo API rejects the request
- **WHEN** Brevo returns a non-success HTTP response for an email request
- **THEN** the sender SHALL throw an exception or return a failed task
- **THEN** `SendMonthlyStatementService` SHALL be able to mark the contact statement as not sent according to its existing partial failure behavior

### Requirement: Email content includes monthly statement details
Monthly statement emails sent through Brevo SHALL include the contact name, statement period, total amount owed, and the full person/month statement breakdown. The breakdown SHALL include each statement line's address, service name, invoice date/period, invoice total, and the recipient's allocated share.

#### Scenario: Recipient receives unified person-month statement email
- **WHEN** a person-month statement email is sent
- **THEN** the email content SHALL include the statement period
- **THEN** the email content SHALL include the contact/person name
- **THEN** the email content SHALL include the total amount owed by that person for the month
- **THEN** the email content SHALL include all address/service invoice lines that make up the total

#### Scenario: Recipient has invoices from multiple addresses
- **WHEN** the monthly statement contains lines from multiple addresses
- **THEN** the Brevo email SHALL contain one unified email body for the person
- **THEN** the email SHALL show the address and service for each invoice line
