## 1. Secret Hygiene Setup

- [x] 1.1 Add `.env`, `.env.*`, and local secret variants to `.gitignore` while preserving `.env.example` as trackable
- [x] 1.2 Add `.env.example` with placeholders for `IDENTITY_CONNECTION_STRING`, `APP_CONNECTION_STRING`, `BREVO_API_KEY`, `BREVO_SENDER_EMAIL`, and `BREVO_SENDER_NAME`
- [x] 1.3 Remove real connection strings and any other secret values from tracked `Web/appsettings.json` and `Web/appsettings.Development.json`
- [x] 1.4 Verify `git status --short` does not show a local `.env` file as trackable

## 2. Environment Configuration Loading

- [x] 2.1 Add a dotenv loading mechanism to the Web startup path, using either a small local parser or a focused package dependency
- [x] 2.2 Load repository-root `.env` values before EF Core contexts and email services are registered
- [x] 2.3 Resolve `IdentityConnection` from `ConnectionStrings:IdentityConnection`, `ConnectionStrings__IdentityConnection`, or `IDENTITY_CONNECTION_STRING`
- [x] 2.4 Resolve `AppConnection` from `ConnectionStrings:AppConnection`, `ConnectionStrings__AppConnection`, or `APP_CONNECTION_STRING`
- [x] 2.5 Validate required database connection strings at startup with clear missing-setting errors

## 3. Brevo Configuration

- [x] 3.1 Add a configuration model or helper for Brevo settings: API key, sender email, and sender name
- [x] 3.2 Resolve Brevo settings from environment-backed configuration variables
- [x] 3.3 Validate required Brevo settings at startup when Brevo email delivery is enabled
- [x] 3.4 Document configuration precedence and required variables in an appropriate project README or configuration comment if one exists

## 4. Brevo Email Sender

- [x] 4.1 Add `BrevoEmailSender` in the Application project implementing `IEmailSender`
- [x] 4.2 Register `HttpClient` support for `BrevoEmailSender` in `Web/Program.cs`
- [x] 4.3 Build Brevo transactional email payloads with configured sender identity, recipient, statement period, and amount owed
- [x] 4.4 Send requests to Brevo using the configured API key without hard-coded credentials
- [x] 4.5 Treat non-success Brevo API responses as send failures with useful exception messages
- [x] 4.6 Replace the default `IEmailSender` registration from `ConsoleEmailSender` to `BrevoEmailSender`
- [x] 4.7 If retaining `ConsoleEmailSender`, gate it behind an explicit development-only provider setting instead of silent fallback

## 5. Verification

- [ ] 5.1 Run `dotnet build invoice-service.sln` and fix compile errors
- [ ] 5.2 Start the Web application with a populated local `.env` and verify both DbContexts resolve their connection strings
- [ ] 5.3 Test startup failure with one required value missing and confirm the error identifies the missing setting
- [ ] 5.4 If valid Brevo credentials are available, send a monthly statement email and verify Brevo accepts the request
- [x] 5.5 Confirm tracked files do not contain real database passwords, Brevo API keys, or email passwords
