## Why

Database connection strings and email credentials are environment-specific secrets and should not live in tracked JSON configuration. The application also needs a real email provider so monthly statements can be delivered through Brevo instead of the current console-only sender.

## What Changes

- Add a local `.env` file workflow for development secrets, including auth database, app database, and Brevo credentials.
- Add a tracked `.env.example` that documents required variables without secret values.
- Add `.env` and environment-specific secret files to `.gitignore` so credentials are not committed.
- Update application configuration startup so environment variables populate `ConnectionStrings:IdentityConnection`, `ConnectionStrings:AppConnection`, and email settings.
- Replace the console-only email sender registration with a Brevo-backed sender for monthly statement emails.
- Keep `AppDbContext` and `AppIdentityDbContext` separate and continue using separate connection strings.
- Remove secret values from tracked appsettings files, leaving only safe defaults or non-secret structure.
- Add startup validation that fails fast when required database or Brevo settings are missing.

## Capabilities

### New Capabilities
- `env-configuration`: Defines how the application loads required database and email settings from environment variables and local `.env` files.
- `brevo-email-delivery`: Defines how monthly statement emails are sent through Brevo using configured credentials.
- `secret-hygiene`: Defines repository safeguards for documenting required secrets while preventing local secret values from being tracked.

### Modified Capabilities
<!-- No existing specs to modify -->

## Impact

- **Web/Program.cs**: Configuration bootstrap, required setting validation, database connection string resolution, and email sender DI registration.
- **Web/appsettings.json / Web/appsettings.Development.json**: Remove committed secret values and keep only safe configuration.
- **Application**: Add a Brevo implementation of `IEmailSender`; optionally keep `ConsoleEmailSender` only for explicit development fallback if needed.
- **Contracts/Application**: `IEmailSender` remains the application boundary for monthly statement email delivery.
- **Project dependencies**: Add a `.env` loader package or small local loader, plus HTTP client support for Brevo if not already available.
- **Repository root**: Add `.env.example`; update `.gitignore` for `.env` and other local secret files.
- **Operations**: Developers and deployment environments must provide required database and Brevo values via environment variables.
