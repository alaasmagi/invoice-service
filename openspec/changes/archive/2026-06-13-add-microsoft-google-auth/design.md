## Context

The app already uses ASP.NET Core Identity with `AppUser` and a separate `AppIdentityDbContext`. Business data is scoped to the authenticated Identity user, so external authentication must create or link normal Identity users instead of introducing a parallel user model.

`Web/Program.cs` already conditionally registers Google and Microsoft authentication handlers from configuration, and `.env.example` documents the expected provider keys. The missing product behavior is a complete, visible Identity UI flow that lets users choose configured providers and completes the external-login callback in a way that preserves existing local sign-in and ownership behavior.

## Goals / Non-Goals

**Goals:**
- Support Google and Microsoft sign-in through ASP.NET Core Identity.
- Display external login buttons on account pages when provider credentials are configured.
- Create or link external logins to `AppUser` records stored by `AppIdentityDbContext`.
- Keep provider secrets in configuration or environment variables.
- Preserve local email/password registration, login, logout, and account management.

**Non-Goals:**
- Replacing ASP.NET Core Identity with OpenID Connect-only authentication.
- Adding role, tenant, or administrator account management.
- Migrating business data ownership keys.
- Requiring Microsoft or Google authentication when the app is deployed without provider credentials.

## Decisions

1. Use ASP.NET Core Identity external login support.

   Identity already owns sign-in cookies, local users, external login records, and user ID generation. Keeping the provider flow inside Identity means existing `UserManager<AppUser>`, `SignInManager<AppUser>`, and `AspNetUserLogins` behavior can be used directly.

   Alternative considered: custom OAuth/OIDC controller flow. This would duplicate Identity callback, anti-forgery, and account-linking logic and increase security risk without improving the product workflow.

2. Keep `AppUser` as the only authenticated user model.

   External providers will authenticate a user, but the app will still authorize and scope data by the local `AppUser.Id`. New external users will get an `AppUser`; returning external users will resolve through Identity external login records.

   Alternative considered: storing provider subject IDs on business entities. That would couple business data to provider-specific identifiers and make multiple provider links harder.

3. Show provider options only when handlers are configured.

   The UI will use `SignInManager.GetExternalAuthenticationSchemesAsync()` so available buttons match configured handlers. If neither provider is configured, the local login/register pages should continue without broken external options.

   Alternative considered: always showing Google and Microsoft buttons and failing later. That creates avoidable user-facing errors in local and partially configured environments.

4. Scaffold/customize only the required Identity account pages.

   The implementation should add the minimum Razor Pages needed for login, registration, external login callback, and external login confirmation if the default package UI cannot satisfy the required UX. The rest of the Identity UI should remain package-provided unless customization is required.

   Alternative considered: scaffold all Identity UI. That adds maintenance surface and unrelated files.

## Risks / Trade-offs

- Provider app callback mismatch -> Document and test callback paths for Google `/signin-google` and Microsoft `/signin-microsoft`.
- Missing email claim from provider -> Require an email during external login confirmation before creating the local `AppUser`.
- Account takeover through email matching -> Do not automatically attach an external provider to an existing local account solely by matching email unless the user is already authenticated or Identity's confirmation flow proves ownership.
- Partially configured credentials -> Treat a provider as unavailable unless both client ID and client secret are non-empty.
- Email confirmation policy interaction -> Ensure external account creation works with the current `RequireConfirmedAccount` setting, either by marking confirmed only when acceptable for trusted providers or by requiring the normal confirmation path.

## Migration Plan

No business-domain migration is expected. Existing Identity schema includes external login tables through ASP.NET Core Identity. Deployment requires setting provider client IDs/secrets in the runtime environment and registering the production callback URLs with Google and Microsoft.

Rollback is configuration-only for provider availability: remove provider credentials to hide external login options. Code rollback should preserve local Identity login because the external flow is additive.

## Open Questions

- Should accounts created through Google or Microsoft be treated as email-confirmed when the provider returns a verified email claim, or should the app always require a local confirmation step?
