## MODIFIED Requirements

### Requirement: Email content includes monthly statement details
Monthly statement emails sent through Brevo SHALL include the contact name, statement period, total amount owed, the full person/month statement breakdown, and sender payment details. The breakdown SHALL include each statement line's address, service name, invoice date/period, invoice total, and the recipient's allocated share. Sender payment details SHALL include the authenticated sender's Identity user fullname as the bank account recipient name and the authenticated sender's Identity user bank IBAN.

#### Scenario: Recipient receives unified person-month statement email
- **WHEN** a person-month statement email is sent
- **THEN** the email content SHALL include the statement period
- **THEN** the email content SHALL include the contact/person name
- **THEN** the email content SHALL include the total amount owed by that person for the month
- **THEN** the email content SHALL include all address/service invoice lines that make up the total
- **THEN** the email content SHALL include the sender's bank account recipient name
- **THEN** the email content SHALL include the sender's bank IBAN

#### Scenario: Recipient has invoices from multiple addresses
- **WHEN** the monthly statement contains lines from multiple addresses
- **THEN** the Brevo email SHALL contain one unified email body for the person
- **THEN** the email SHALL show the address and service for each invoice line
