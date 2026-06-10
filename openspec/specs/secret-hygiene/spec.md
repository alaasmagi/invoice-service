## ADDED Requirements

### Requirement: Required secrets are documented without values
The repository SHALL include a tracked `.env.example` file that documents every environment variable required to run the application without containing real secret values.

#### Scenario: Developer prepares local environment
- **WHEN** a developer opens `.env.example`
- **THEN** it SHALL list variables for the identity database connection string, application database connection string, Brevo API key, Brevo sender email, and Brevo sender name
- **THEN** it SHALL use placeholders instead of deployable credentials

### Requirement: Local dotenv files are ignored by git
The repository SHALL ignore local dotenv files that may contain secrets, including `.env` and environment-specific dotenv variants.

#### Scenario: Developer creates local dotenv file
- **WHEN** a developer creates `.env` with database and Brevo credentials
- **THEN** git SHALL not include `.env` as a tracked change

### Requirement: Secret-bearing config is not reintroduced
Future configuration changes SHALL keep secret values out of tracked source files and use environment-backed configuration for secret-bearing settings.

#### Scenario: New secret setting is added
- **WHEN** a new secret-bearing setting is required
- **THEN** it SHALL be added to `.env.example` with a placeholder
- **THEN** the real value SHALL be supplied through `.env` or host environment variables
