## 1. Persistence and Model Cleanup

- [x] 1.1 Convert `Domain/AppUser.cs` from ASP.NET Core Identity inheritance to a local invoice-service profile domain model with id, full name, and bank IBAN.
- [x] 1.2 Ensure `DTO/DataAccess/DTO/AppUserEntity.cs` represents the persisted local app user profile and uses the same id as the identity-hub `nameidentifier` claim.
- [x] 1.3 Add explicit AppUser domain/entity mapper support.
- [x] 1.4 Add `DbSet<AppUserEntity>` and EF configuration for app users to `AppDbContext`.
- [x] 1.5 Verify every `AppDbContext` DbSet and relationship maps DataAccess entity classes rather than domain classes.
- [x] 1.6 Remove `AppIdentityDbContext` and local Identity migrations from the active code path.
- [x] 1.7 Create/update AppDbContext migration artifacts for app user profile persistence and removal of local identity-context dependencies.

## 2. Identity-Hub Client Infrastructure

- [x] 2.1 Add identity-hub options for base URL, client id, client secret, and optional callback URL.
- [x] 2.2 Update required configuration validation and `.env.example` for identity-hub settings and removal of local identity/provider secret requirements.
- [x] 2.3 Add identity-hub API request/response DTOs for local login, external challenge, token exchange, claims, and error/next-step responses.
- [x] 2.4 Implement an `IIdentityHubClient` service using `HttpClient` for `/api/auth/login`, `/api/auth/external/challenge`, and `/api/auth/external/token/exchange`.
- [x] 2.5 Ensure identity-hub client logging/error handling never exposes the client secret.

## 3. Cookie Authentication and Razor Flow

- [x] 3.1 Replace `AddDefaultIdentity`, `UserManager`, `SignInManager`, and local external provider registration with cookie authentication.
- [x] 3.2 Add local MVC/Razor login route that redirects to identity-hub `/Identity/Account/Login` with `clientId` and `redirectUri`.
- [x] 3.3 Add local auth callback route that handles `code` and `error` query parameters.
- [x] 3.4 Exchange callback codes for identity-hub claims, validate the `nameidentifier` claim, upsert the local app user profile, and sign in with the local cookie.
- [x] 3.5 Add local logout route that clears the invoice-service auth cookie.
- [x] 3.6 Replace Identity area links and `_LoginPartial` dependencies with the new local login, logout, and profile routes.

## 4. API Authentication Flow

- [x] 4.1 Add invoice-service API endpoints or handlers for local email/password login that call identity-hub and create a local session only after auth-code exchange.
- [x] 4.2 Add invoice-service API endpoint or handler for external provider challenge that returns identity-hub redirect information.
- [x] 4.3 Return structured API responses for success, two-factor required, consent required, invalid client, invalid redirect URI, invalid client secret, and general auth failure.
- [x] 4.4 Confirm API auth responses do not leak identity-hub secrets or raw unexpected exception details.

## 5. Profile and Ownership Updates

- [x] 5.1 Replace `AccountProfileController` `UserManager<AppUser>` usage with repository/service access to the local app user profile.
- [x] 5.2 Update monthly statement sender payment details lookup to read full name and bank IBAN from the local app user profile.
- [x] 5.3 Verify `CurrentUserId()` and all user-scoped controllers continue using the signed-in `ClaimTypes.NameIdentifier` claim.
- [x] 5.4 Ensure first sign-in creates a local app user profile before profile or monthly statement workflows require it.

## 6. Removal and Verification

- [x] 6.1 Delete unused Identity DTOs, mappers, context registrations, usings, package references, and views/pages that only supported local Identity.
- [x] 6.2 Remove local Google/Microsoft auth configuration handling from invoice-service.
- [ ] 6.3 Build the solution and fix compile errors caused by Identity removal.
- [ ] 6.4 Run available tests or add focused tests for identity-hub client response handling, callback sign-in, app user upsert, and profile payment details.
- [x] 6.5 Run `openspec validate integrate-identity-hub-dataaccess-entities --strict`.
