## ADDED Requirements

### Requirement: Application loads local dotenv configuration
The Web application SHALL load key-value pairs from a repository-root `.env` file during startup when the file exists. Missing `.env` files SHALL NOT prevent startup if equivalent environment variables are already provided by the host.

#### Scenario: Local dotenv file exists
- **WHEN** the application starts and a `.env` file exists at the repository root
- **THEN** the application SHALL add the `.env` values to its configuration sources
- **THEN** values from `.env` SHALL be available before database contexts and email services are registered

#### Scenario: Host provides environment variables without dotenv file
- **WHEN** the application starts and no `.env` file exists
- **THEN** the application SHALL continue using environment variables provided by the host process

### Requirement: Database connection strings come from environment-backed configuration
The Web application SHALL resolve the Identity database connection string and application database connection string from environment-backed configuration. It SHALL preserve separate values for `AppIdentityDbContext` and `AppDbContext`.

#### Scenario: Flat database variables are provided
- **WHEN** `IDENTITY_CONNECTION_STRING` and `APP_CONNECTION_STRING` are configured
- **THEN** `AppIdentityDbContext` SHALL use the identity connection string
- **THEN** `AppDbContext` SHALL use the application connection string

#### Scenario: ASP.NET Core connection string keys are provided
- **WHEN** `ConnectionStrings__IdentityConnection` and `ConnectionStrings__AppConnection` are configured
- **THEN** the application SHALL resolve them as valid database connection strings

### Requirement: Required configuration is validated at startup
The Web application SHALL validate required database and Brevo configuration during startup and fail with a clear error when any required value is missing or blank.

#### Scenario: Identity connection string is missing
- **WHEN** the application starts without an identity connection string
- **THEN** startup SHALL fail with an error identifying the missing identity database configuration

#### Scenario: Brevo API key is missing
- **WHEN** the application starts with Brevo email delivery enabled and no Brevo API key
- **THEN** startup SHALL fail with an error identifying the missing Brevo API key

### Requirement: Tracked appsettings files do not contain secret values
Tracked `appsettings.json` and `appsettings.Development.json` files SHALL NOT contain real database passwords, Brevo API keys, SMTP passwords, or other deployable secret values.

#### Scenario: Developer inspects tracked appsettings
- **WHEN** a developer opens tracked appsettings files
- **THEN** the files SHALL contain only safe defaults, placeholders, or non-secret configuration

### Requirement: Bank details are not required startup configuration
The Web application SHALL NOT require bank account name or bank IBAN values from environment-backed configuration during startup.

#### Scenario: Bank environment variables are absent
- **WHEN** the application starts without `BANK_ACCOUNT_NAME` or `BANK_IBAN`
- **THEN** startup SHALL continue if database and Brevo configuration are otherwise valid

#### Scenario: Bank environment variables are present
- **WHEN** the application starts with legacy `BANK_ACCOUNT_NAME` or `BANK_IBAN` values present
- **THEN** startup SHALL ignore those values for monthly statement payment details
