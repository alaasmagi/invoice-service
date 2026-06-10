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
Monthly statement emails sent through Brevo SHALL include the contact name, statement period, and amount owed.

#### Scenario: Recipient receives statement email
- **WHEN** a monthly statement email is sent
- **THEN** the email content SHALL include the statement period
- **THEN** the email content SHALL include the amount owed by the contact
