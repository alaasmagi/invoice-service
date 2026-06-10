## ADDED Requirements

### Requirement: Bank details are not required startup configuration
The Web application SHALL NOT require bank account name or bank IBAN values from environment-backed configuration during startup.

#### Scenario: Bank environment variables are absent
- **WHEN** the application starts without `BANK_ACCOUNT_NAME` or `BANK_IBAN`
- **THEN** startup SHALL continue if database and Brevo configuration are otherwise valid

#### Scenario: Bank environment variables are present
- **WHEN** the application starts with legacy `BANK_ACCOUNT_NAME` or `BANK_IBAN` values present
- **THEN** startup SHALL ignore those values for monthly statement payment details
