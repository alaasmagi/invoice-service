## Context

Monthly statement emails currently render payment details from `BrevoEmailOptions`, which are populated from environment variables. That makes bank details global for the deployment. The application already uses ASP.NET Core Identity with `Domain.AppUser` and `AppIdentityDbContext`, and `AppUser` already contains `Fullname`. Monthly statement sending is initiated by the authenticated user and already receives the current user's id, so the sender identity can be loaded from the Identity database during the send flow.

The app keeps `AppIdentityDbContext` and `AppDbContext` separate. This change must preserve that separation: business records remain in `AppDbContext`, while sender profile/payment fields live on `AppUser` in `AppIdentityDbContext`.

## Goals / Non-Goals

**Goals:**

- Store bank IBAN on `Domain.AppUser`.
- Let a signed-in user fill or update their bank IBAN from the UI.
- Use `AppUser.Fullname` as the bank account recipient name in monthly statement emails.
- Use `AppUser.BankIban` as the IBAN in monthly statement emails.
- Fail monthly statement sending with a clear message if the sender has no bank IBAN configured.
- Remove bank account name and IBAN from required startup configuration.
- Add an Identity database migration for the new user field.

**Non-Goals:**

- Moving recipient contacts into Identity.
- Allowing one user to edit another user's bank details.
- Storing bank details in the application business database.
- Supporting multiple bank accounts per user.
- Validating every global IBAN format beyond basic non-empty/length/format checks in this change.
- Changing how monthly statement totals or invoice allocations are calculated.

## Decisions

- Add `BankIban` to `Domain.AppUser` rather than creating a new business entity.
  - Rationale: The bank account belongs to the authenticated sender, not to an invoice, address, or contact.
  - Alternative considered: Store bank details in `.env` or `AppDbContext`. Rejected because `.env` is deployment-wide and `AppDbContext` would duplicate Identity profile state.

- Use `AppUser.Fullname` as the bank account name.
  - Rationale: The user already requested the account name come from the existing Identity user fullname.
  - Alternative considered: Add a separate account-name field. Rejected because it adds extra profile state and contradicts the requested behavior.

- Load sender bank details in `SendMonthlyStatementService` through an application-facing identity profile service.
  - Rationale: The Application layer should not depend directly on ASP.NET Identity managers or Web concerns. A small contract keeps the BLL testable and lets Web/DataAccess provide the Identity-backed implementation.
  - Alternative considered: Read Identity user directly in the MVC controller and pass bank details into the send service. Rejected because sending rules belong in the application service and details-page/index-page sends should behave consistently.

- Add bank details to `MonthlyStatementEmail` rather than keeping them in `BrevoEmailOptions`.
  - Rationale: Payment details vary by sender and by request; provider options should only contain Brevo provider credentials and sender email identity.
  - Alternative considered: Mutate `BrevoEmailOptions` per request. Rejected because options are registered as singleton configuration and should not carry per-user state.

- Keep `.env` support for database and Brevo settings only.
  - Rationale: payment details are no longer deployment secrets/configuration; they are user profile data.
  - Alternative considered: Continue allowing `.env` fallback if user IBAN is missing. Rejected because fallback would hide incomplete user profile setup and reintroduce global payment details.

## Risks / Trade-offs

- Existing users have no `BankIban` after migration -> Send action returns a clear status message and the profile page prompts the user to fill IBAN.
- Identity UI is currently mostly default scaffolded UI -> Add the minimal account management page/model changes needed instead of redesigning Identity.
- `Fullname` may be blank on older users -> Send validation should treat missing fullname as a profile setup error alongside missing IBAN.
- Removing bank values from required startup config may leave stale `.env` keys -> Keep stale keys harmless and update `.env.example`; do not fail startup because they exist.
- Separate DbContexts can make cross-database querying tempting -> Load Identity user through a dedicated service by user id and keep business queries in existing repositories.
