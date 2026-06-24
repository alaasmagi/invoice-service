## Context

invoice-service currently owns local ASP.NET Core Identity through `AppIdentityDbContext`, `AppUser : IdentityUser`, Identity Razor pages, local external provider registration, and Identity-specific migrations. The app also already has DataAccess DTO/entity classes and explicit mappers, and most repositories work through `AppDbContext`.

identity-hub is now the authoritative authentication system. invoice-service should become a client that redirects browser users to identity-hub or calls identity-hub auth APIs, exchanges auth codes for claims, and creates its own local cookie session. invoice-service still needs local profile data for monthly statement emails, specifically sender full name and bank IBAN.

## Goals / Non-Goals

**Goals:**
- Remove local Identity persistence and provider setup from invoice-service.
- Implement identity-hub Razor browser flow: login redirect, callback, code exchange, local cookie sign-in, error display, and logout.
- Implement identity-hub API client flow with request/response DTOs for local login, external challenge, auth-code exchange, and common errors.
- Store invoice-service profile data in the app database through `AppUserEntity`.
- Ensure `AppDbContext` maps DataAccess entity classes directly, including `AppUserEntity`, and repositories continue exposing domain models.
- Preserve existing ownership scoping by using the identity-hub `nameidentifier` claim as the stable local user id.

**Non-Goals:**
- Do not implement identity-hub itself or change identity-hub database/client records.
- Do not add local password registration, password reset, 2FA, or provider linking to invoice-service.
- Do not replace repository and application service boundaries with direct DbContext access from Web.
- Do not migrate historical local Identity passwords or external login associations into identity-hub as part of this change.

## Decisions

1. Use cookie authentication directly in invoice-service.

   invoice-service will configure `CookieAuthenticationDefaults.AuthenticationScheme`, set `LoginPath` to the local login route, and remove `AddDefaultIdentity`, `SignInManager`, `UserManager`, provider-specific auth handlers, and `AppIdentityDbContext`. This matches identity-hub documentation: identity-hub authenticates the user, then the client app creates its own local auth cookie from exchanged claims.

   Alternative considered: keep ASP.NET Core Identity locally only for cookie/session management. That retains the unwanted identity database and keeps invoice-service coupled to Identity user stores.

2. Preserve the identity-hub subject as the local user id.

   The `ClaimTypes.NameIdentifier` returned by identity-hub will remain the id used by `CurrentUserId()` and all existing ownership filters. On successful callback or API auth-code exchange, invoice-service will upsert a local `AppUserEntity` row with that id and any available profile claims, without creating local credentials.

   Alternative considered: create new invoice-service-only user ids and map them to identity-hub subjects. That adds a mapping table and increases the risk of broken ownership scoping.

3. Keep a local AppUser profile but remove Identity inheritance.

   The domain `AppUser` should become an invoice-service profile model instead of an ASP.NET Identity entity. `AppUserEntity` will be persisted by `AppDbContext` and will contain `Id`, `Fullname`, `BankIban`, and DataAccess metadata/concurrency fields if required by the existing base types.

   Alternative considered: read full name and bank IBAN from identity-hub. That does not satisfy the current invoice-service-specific bank details workflow and would push invoice payment data into the auth system.

4. Use a typed identity-hub client service.

   Web auth handlers and any invoice-service API auth endpoints will call an `IIdentityHubClient` abstraction. It will own the HTTP calls and DTOs for:
   - Razor callback code exchange through `/api/auth/external/token/exchange`
   - API local login through `/api/auth/login`
   - API external challenge through `/api/auth/external/challenge`
   - common identity-hub error responses

   Alternative considered: call `HttpClient` directly from controllers/pages. That duplicates endpoint details and makes API response/error handling inconsistent.

5. Keep EF Core mapped to DataAccess entities.

   `AppDbContext` will expose `DbSet<...Entity>` sets only. Domain models remain pure business objects used by application services and controllers where appropriate. Repositories will continue translating explicitly through mappers.

   Alternative considered: map domain entities directly in EF and remove DataAccess entity DTOs. That conflicts with the requested direction and the existing DTO/mapper structure.

## Risks / Trade-offs

- [Risk] identity-hub may return a non-GUID `nameidentifier` claim while current ownership code requires a GUID. -> Mitigate by validating the claim during sign-in, failing with a clear auth error, and confirming identity-hub client configuration uses GUID user ids before rollout.
- [Risk] Removing local Identity can break generated `_LoginPartial`, Identity area routes, account management links, and profile controllers. -> Mitigate by replacing Identity UI references with MVC auth/profile routes and compile-checking all Web views.
- [Risk] Existing app data may reference local Identity user ids that do not exist in identity-hub. -> Mitigate by requiring identity-hub users to preserve the same subject ids or by planning a one-time data migration before production deployment.
- [Risk] Deleting identity migrations/history is destructive for local auth data. -> Mitigate by treating local Identity auth data as superseded by identity-hub and backing up databases before removing the context from deployment.
- [Risk] API login flows can surface `requiresTwoFactor` or `requiresConsent`, which invoice-service may not implement locally. -> Mitigate by returning structured responses telling the client to continue those flows in identity-hub, and using the Razor identity-hub flow for consent-required cases.
