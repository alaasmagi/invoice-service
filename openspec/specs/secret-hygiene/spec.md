## Purpose
Define how secrets are documented, configured and kept out of source control, logs and responses.
## Requirements
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

### Requirement: Auth service secrets are not exposed
The system SHALL treat the identity-hub client secret used for auth-service calls as a secret-bearing configuration value.

#### Scenario: Auth service credentials are logged
- **WHEN** auth-service options, request headers, authentication configuration, or exceptions are logged
- **THEN** logs SHALL NOT include `ClientSecret`

#### Scenario: Auth service configuration is returned to clients
- **WHEN** any controller response is serialized
- **THEN** the response SHALL NOT include `ClientSecret`

#### Scenario: Auth service secret placeholders are not duplicated
- **WHEN** secret-bearing environment documentation is updated for auth-service integration
- **THEN** it SHALL reuse the identity-hub client secret placeholder instead of documenting a second auth-service secret

