## Why

Users should be able to sign in with accounts they already use instead of creating and managing a separate local password for this invoice service. Microsoft and Google are the expected identity providers for household and productivity users, and supporting them reduces sign-in friction while keeping ASP.NET Core Identity as the app's authorization and ownership boundary.

## What Changes

- Add Microsoft and Google as supported external authentication providers for the MVC/Identity UI.
- Show external sign-in options on the login and registration flows only when each provider is configured.
- Link successful external logins to the existing `AppUser` Identity model so all existing user-scoped business records remain isolated by Identity user ID.
- Keep provider client IDs and secrets in configuration/environment variables; do not store provider secrets in source-controlled files.
- Preserve existing local Identity registration and sign-in behavior unless a provider is explicitly selected by the user.

## Capabilities

### New Capabilities
- `external-identity-authentication`: Authentication with configured Microsoft and Google external login providers through ASP.NET Core Identity.

### Modified Capabilities

## Impact

- Affects `Web` Identity UI pages, authentication registration in `Program.cs`, and configuration helpers/documentation.
- May require Identity UI scaffolding or customization for login, external login callback, and confirmation pages.
- Adds or verifies package references for the Google and Microsoft Account authentication handlers.
- Uses the existing `AppIdentityDbContext` and Identity tables for external login records; no business-domain schema changes are expected.
