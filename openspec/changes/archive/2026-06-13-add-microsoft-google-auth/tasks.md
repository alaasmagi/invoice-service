## 1. Provider Setup

- [x] 1.1 Verify `Web` references the Google and Microsoft Account authentication packages.
- [x] 1.2 Verify `Program.cs` registers Google and Microsoft handlers only when both client ID and client secret are configured.
- [x] 1.3 Verify `.env.example` documents the provider keys and callback paths for local configuration.

## 2. Identity UI

- [x] 2.1 Scaffold or add the minimal Identity account pages needed for login, registration, external login callback, and external login confirmation.
- [x] 2.2 Render external provider buttons from `SignInManager.GetExternalAuthenticationSchemesAsync()` on login and registration pages.
- [x] 2.3 Ensure providers with incomplete configuration are not shown on account pages.
- [x] 2.4 Preserve the existing local registration, login, logout, and account management links and behavior.

## 3. External Login Flow

- [x] 3.1 Implement the external login challenge flow for selected providers with a local return URL.
- [x] 3.2 Implement callback handling for returning users linked through Identity external login records.
- [x] 3.3 Implement first-time external login confirmation that creates an `AppUser` and stores the provider association.
- [x] 3.4 Handle missing provider email or required local account fields by requiring the user to provide them before account creation.
- [x] 3.5 Decide and implement how external-provider email confirmation interacts with the current `RequireConfirmedAccount` setting.

## 4. Ownership and Regression Checks

- [x] 4.1 Verify users authenticated through Google or Microsoft use the local `AppUser.Id` for all existing user-scoped controllers.
- [x] 4.2 Verify a returning external login does not create a duplicate `AppUser`.
- [x] 4.3 Verify local Identity users can still register, log in, log out, and manage bank details when no external providers are configured.

## 5. Validation

- [ ] 5.1 Build the solution with `dotnet build`.
- [ ] 5.2 Run available automated tests, if any exist.
- [ ] 5.3 Manually smoke-test login page rendering with no providers configured and with mock/local provider configuration present.
