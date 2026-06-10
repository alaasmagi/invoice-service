## Context

The Web project currently reads two PostgreSQL connection strings from `ConnectionStrings:IdentityConnection` and `ConnectionStrings:AppConnection` in ASP.NET Core configuration. `Program.cs` uses those values to register `AppIdentityDbContext` and `AppDbContext`, preserving the required separation between Identity data and application data. Monthly statement sending already depends on `IEmailSender`, but the registered implementation is `ConsoleEmailSender`, so no real email is sent.

This change introduces environment-backed configuration for local development and deployment. It must remove secrets from tracked JSON files, document required settings, load local `.env` values during development, and provide a Brevo-backed `IEmailSender` implementation without changing the domain model or service boundary.

## Goals / Non-Goals

**Goals:**
- Load required database and email credentials from environment variables, including local `.env` files for development.
- Preserve `ConnectionStrings:IdentityConnection` and `ConnectionStrings:AppConnection` as the effective configuration keys consumed by EF Core.
- Add a Brevo email sender that implements the existing `IEmailSender` interface.
- Validate required settings at startup with clear error messages.
- Keep tracked configuration free of real secret values.
- Document all required variables in `.env.example`.

**Non-Goals:**
- Changing the domain model, DTO model, repository structure, or service interface shape.
- Merging `AppDbContext` and `AppIdentityDbContext`.
- Building an email template management system.
- Adding background queues, retries beyond simple HTTP error handling, or delivery webhooks.
- Managing production secrets directly in the repository.

## Decisions

### 1. Use environment variables as the source of truth

**Decision**: Required secrets will be represented as environment variables. Local development may place them in a root `.env` file, which is loaded before building service registrations. Deployment environments must provide equivalent environment variables directly.

**Rationale**: Environment variables are the common denominator for local, CI, container, and hosted deployments. A local `.env` file improves developer ergonomics without requiring secrets in `appsettings*.json`.

**Alternative considered**: Keep secrets in `appsettings.Development.json`. Rejected because the file is tracked and already risks credential leakage.

### 2. Map flat `.env` variables into ASP.NET Core configuration keys

**Decision**: `.env.example` will document flat names such as `IDENTITY_CONNECTION_STRING`, `APP_CONNECTION_STRING`, `BREVO_API_KEY`, `BREVO_SENDER_EMAIL`, and `BREVO_SENDER_NAME`. Startup code will map these to typed configuration options and, where needed, to the existing `ConnectionStrings:*` keys.

**Rationale**: Flat names are easier to use in `.env` files and most hosting platforms. Keeping the effective `ConnectionStrings:*` keys avoids broad changes to EF Core setup.

**Alternative considered**: Use double-underscore keys directly (`ConnectionStrings__IdentityConnection`). This is valid for ASP.NET Core, but less readable in `.env`. The implementation may still support both by preferring existing ASP.NET Core configuration and falling back to flat aliases.

### 3. Add minimal configuration option types

**Decision**: Add option classes for database and Brevo settings, or use strongly named helper methods, so startup validation is explicit and centralized. Required values must fail fast during application startup instead of failing later during database access or email sending.

**Rationale**: Central validation gives developers immediate, actionable feedback when `.env` is incomplete.

**Alternative considered**: Read configuration ad hoc in `BrevoEmailSender` and DbContext registration. Rejected because missing settings would fail later and in inconsistent ways.

### 4. Implement Brevo through `HttpClient`

**Decision**: Create `BrevoEmailSender` in the Application layer that implements `IEmailSender` and sends monthly statement emails through Brevo's transactional email API using `HttpClient` registered by DI.

**Rationale**: `IEmailSender` is already the application boundary. `HttpClient` avoids introducing a provider-specific SDK unless the project later needs advanced Brevo features.

**Alternative considered**: Add Brevo's SDK. Rejected for this stage because simple transactional sending only needs one HTTP endpoint and a small payload.

### 5. Keep `ConsoleEmailSender` only as an explicit development fallback if retained

**Decision**: Production and normal development configuration should register `BrevoEmailSender`. If `ConsoleEmailSender` remains, it must only be used behind an explicit setting such as `EMAIL_PROVIDER=Console`, not as a silent fallback when Brevo is misconfigured.

**Rationale**: Silent fallback can make Send actions appear successful while no real email is delivered.

**Alternative considered**: Always fall back to console in Development. Rejected because it hides missing Brevo configuration and makes end-to-end email testing unreliable.

## Risks / Trade-offs

- **Existing exposed credentials may already be compromised** -> Rotate any database or email credentials that were committed or shared before this change.
- **`.env` loader dependency adds startup code** -> Keep the loader small and isolated to Web startup; prefer a mature package or simple parser with tests if a package is not used.
- **Brevo API contract can change** -> Keep the sender isolated behind `IEmailSender` so provider-specific changes stay in one class.
- **Email send failures affect statement sending** -> Let `SendMonthlyStatementService` continue to handle per-contact failures and status updates; `BrevoEmailSender` should throw clear exceptions for failed API calls.
- **Configuration precedence can be confusing** -> Document precedence and implement predictable behavior: existing ASP.NET Core environment keys first, flat `.env` aliases second, tracked JSON last for non-secret defaults only.

## Migration Plan

1. Add `.env.example` with all required keys and placeholder values.
2. Add `.env` and local secret variants to `.gitignore`.
3. Remove real secret values from tracked `appsettings*.json` files.
4. Add `.env` loading and startup validation in `Web/Program.cs`.
5. Add `BrevoEmailSender` and register it as the default `IEmailSender`.
6. Create a local `.env` with valid database and Brevo values.
7. Run `dotnet build` and, if credentials are available, send a test monthly statement.

Rollback: restore the previous sender registration and connection string configuration temporarily, but do not reintroduce real secrets into tracked files. Prefer setting the same previous values through environment variables instead.

## Open Questions

- Should development support an explicit `EMAIL_PROVIDER=Console` mode for working offline, or should Brevo always be required?
- What sender email and sender display name should Brevo use for monthly statement emails?
- Should monthly statement email content remain plain text initially, or include simple HTML formatting?
