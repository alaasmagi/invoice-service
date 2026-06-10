## 1. Identity Profile Data

- [x] 1.1 Add `BankIban` to `Domain.AppUser`.
- [x] 1.2 Add bank IBAN fields to AppUser DTOs/mappers if they are still used by the project.
- [x] 1.3 Add an `AppIdentityDbContext` migration that adds the bank IBAN column to the auth user table.

## 2. Profile UI

- [x] 2.1 Add or update Identity account/profile UI so a signed-in user can view and edit their bank IBAN.
- [x] 2.2 Validate bank IBAN as required for saving bank details and normalize whitespace/casing before persistence.
- [x] 2.3 Ensure profile updates only affect the authenticated user's own Identity record.

## 3. Application Contracts and BLL

- [x] 3.1 Add a contract/model for loading monthly statement sender payment details from Identity by user id.
- [x] 3.2 Implement the Identity-backed sender payment detail loader without mixing `AppDbContext` and `AppIdentityDbContext` queries.
- [x] 3.3 Update `SendMonthlyStatementService` to require sender fullname and bank IBAN before sending.
- [x] 3.4 Add sender bank account name and IBAN to `MonthlyStatementEmail` payload.

## 4. Email and Configuration

- [x] 4.1 Update Brevo and console email senders to render payment details from `MonthlyStatementEmail` instead of `BrevoEmailOptions`.
- [x] 4.2 Remove bank account name and IBAN from `BrevoEmailOptions` and required startup configuration validation.
- [x] 4.3 Remove or deprecate `BANK_ACCOUNT_NAME` and `BANK_IBAN` from `.env.example`.
- [x] 4.4 Keep legacy `.env` bank keys harmless when present but unused.

## 5. User Feedback

- [x] 5.1 Show a clear Monthly Statement send failure message when sender fullname or bank IBAN is missing.
- [x] 5.2 Ensure resend behavior uses the latest saved sender bank details.

## 6. Verification

- [x] 6.1 Add tests for missing sender bank details blocking monthly statement send.
- [x] 6.2 Add tests or focused coverage that email payloads include sender fullname and IBAN.
- [x] 6.3 Run build and relevant tests, or document any environment blocker.
