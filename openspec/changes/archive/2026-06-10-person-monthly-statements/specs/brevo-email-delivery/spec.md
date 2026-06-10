## MODIFIED Requirements

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
