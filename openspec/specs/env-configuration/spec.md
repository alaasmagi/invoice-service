## Purpose
Define required environment-backed configuration for invoice-service startup, identity-hub client integration, database access, and secret hygiene.

## Requirements

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
The Web application SHALL resolve the application database connection string from environment-backed configuration. It SHALL NOT require a separate identity database connection string after identity-hub authentication is integrated.

#### Scenario: Flat app database variable is provided
- **WHEN** `APP_CONNECTION_STRING` is configured
- **THEN** `AppDbContext` SHALL use the application connection string

#### Scenario: ASP.NET Core app connection string key is provided
- **WHEN** `ConnectionStrings__AppConnection` is configured
- **THEN** the application SHALL resolve it as a valid database connection string

#### Scenario: Legacy identity connection string is absent
- **WHEN** the application starts without `IDENTITY_CONNECTION_STRING` or `ConnectionStrings__IdentityConnection`
- **THEN** startup SHALL continue if the application database and identity-hub settings are otherwise valid

### Requirement: Required configuration is validated at startup
The Web application SHALL validate required application database, identity-hub, and Brevo configuration during startup and fail with a clear error when any required value is missing or blank.

#### Scenario: App connection string is missing
- **WHEN** the application starts without an application database connection string
- **THEN** startup SHALL fail with an error identifying the missing application database configuration

#### Scenario: Identity-hub configuration is missing
- **WHEN** the application starts without required identity-hub client settings
- **THEN** startup SHALL fail with an error identifying the missing identity-hub configuration

#### Scenario: Brevo API key is missing
- **WHEN** the application starts with Brevo email delivery enabled and no Brevo API key
- **THEN** startup SHALL fail with an error identifying the missing Brevo API key

### Requirement: Tracked appsettings files do not contain secret values
Tracked `appsettings.json`, `appsettings.Development.json`, and `.env.example` files SHALL NOT contain real database passwords, identity-hub client secrets, Brevo API keys, SMTP passwords, or other deployable secret values.

#### Scenario: Developer inspects tracked configuration templates
- **WHEN** a developer opens tracked appsettings files or `.env.example`
- **THEN** the files SHALL contain only safe defaults, placeholders, or non-secret configuration

### Requirement: Bank details are not required startup configuration
The Web application SHALL NOT require bank account name or bank IBAN values from environment-backed configuration during startup.

#### Scenario: Bank environment variables are absent
- **WHEN** the application starts without `BANK_ACCOUNT_NAME` or `BANK_IBAN`
- **THEN** startup SHALL continue if database and Brevo configuration are otherwise valid

#### Scenario: Bank environment variables are present
- **WHEN** the application starts with legacy `BANK_ACCOUNT_NAME` or `BANK_IBAN` values present
- **THEN** startup SHALL ignore those values for monthly statement payment details

### Requirement: Identity-hub client settings are required
The Web application SHALL require identity-hub client settings during startup.

#### Scenario: Identity-hub settings are configured
- **WHEN** `IdentityHub:BaseUrl`, `IdentityHub:ClientId`, and `IdentityHub:ClientSecret` are configured
- **THEN** the application SHALL use those values for identity-hub redirects and token exchange calls

#### Scenario: Identity-hub base URL is missing
- **WHEN** the application starts without an identity-hub base URL
- **THEN** startup SHALL fail with an error identifying the missing identity-hub base URL configuration

#### Scenario: Identity-hub client secret is missing
- **WHEN** the application starts without an identity-hub client secret
- **THEN** startup SHALL fail with an error identifying the missing identity-hub client secret configuration

### Requirement: Identity-hub callback URL is configurable
The Web application SHALL resolve the identity-hub callback URL from configuration or from the current request when building login/API requests.

#### Scenario: Explicit callback URL is configured
- **WHEN** `IdentityHub:CallbackUrl` or `IDENTITY_CALLBACK_URL` is configured
- **THEN** the application SHALL use that value as the redirect URI sent to identity-hub

#### Scenario: Explicit callback URL is absent
- **WHEN** no explicit callback URL is configured and a browser login request is active
- **THEN** the application SHALL generate the callback URL from the current forwarded request scheme, host, and callback route
