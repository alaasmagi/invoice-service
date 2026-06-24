## Why

The app currently owns ASP.NET Core Identity state locally, including a separate identity DbContext and identity migrations, while identity-hub is now the central authentication provider. Moving authentication to identity-hub removes duplicate identity infrastructure and lets invoice-service keep only the local user profile fields it needs for invoice workflows.

## What Changes

- Replace local ASP.NET Core Identity login, registration, external provider, callback, and logout behavior with identity-hub client integration.
- Add invoice-service Razor redirects to identity-hub login and callback handling that exchanges identity-hub auth codes for claims and creates the local invoice-service auth cookie.
- Add API/client support for identity-hub login, external provider challenge, auth-code exchange, error handling, and response DTOs.
- Keep a local `AppUser` record for invoice-service-owned profile data: `Fullname` and `BankIban`.
- **BREAKING** Remove the separate local identity database context, local Identity entities, Identity DTOs, Identity mappers, and local Identity migrations.
- Rework `AppDbContext` so EF Core maps DataAccess DTO/entity classes directly rather than domain entities, including `AppUserEntity`.
- Preserve domain models for business logic and keep repositories/mappers responsible for translating between domain objects and DataAccess entities.
- Update configuration requirements for `IdentityHub:BaseUrl`, `IdentityHub:ClientId`, `IdentityHub:ClientSecret`, and callback URL/origin behavior.

## Capabilities

### New Capabilities
- `dataaccess-entity-persistence`: AppDbContext persistence uses DataAccess entity classes as EF models, including the local app user profile entity.

### Modified Capabilities
- `external-identity-authentication`: Authentication is delegated to identity-hub instead of local ASP.NET Core Identity and in-app external provider configuration.
- `identity-user-bank-details`: Sender full name and bank IBAN are stored on the local invoice-service AppUser profile associated with identity-hub claims.
- `env-configuration`: Required configuration includes identity-hub client settings and no longer requires local identity provider settings for invoice-service auth.

## Impact

- Affected projects: `Web`, `DataAccess`, `DTO`, `Domain`, `Application`, and `Contracts`.
- Affected auth surface: login redirect, auth callback, logout, auth cookie creation, current user lookup, API auth endpoints/responses, and existing authorization/user-scoping.
- Affected persistence: `AppDbContext`, migrations, repository DbSet usage, DataAccess entities, mappers, and removal of `AppIdentityDbContext`.
- Affected configuration/deployment: environment variables for identity-hub base URL, client ID, client secret, callback URL, and identity-hub `Clients.AllowedOrigins`.
