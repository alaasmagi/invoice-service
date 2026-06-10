## Why

Monthly statement payment details are currently configured globally through `.env`, which makes every user's outgoing statement emails use the same bank account. Bank payment details should belong to the authenticated Identity user who sends the monthly statement, so each user's recipients see that user's own account name and IBAN.

## What Changes

- Add bank IBAN as an editable field on the Identity user profile.
- Use the Identity user's existing `Fullname` as the bank account recipient name in monthly statement emails.
- Use the Identity user's saved bank IBAN in monthly statement emails instead of `BANK_IBAN` from environment configuration.
- Stop requiring `BANK_ACCOUNT_NAME` and `BANK_IBAN` during application startup.
- Add validation so monthly statement sending fails clearly when the sending user has no bank IBAN configured.
- Add Identity database migration for the new user bank IBAN field.
- No breaking changes to existing invoices, monthly statement records, or recipient contact data.

## Capabilities

### New Capabilities
- `identity-user-bank-details`: Store and edit sender bank IBAN on the authenticated Identity user profile.

### Modified Capabilities
- `brevo-email-delivery`: Monthly statement emails use sender Identity user payment details instead of environment-configured bank details.
- `env-configuration`: Bank account name and IBAN are no longer required environment-backed startup configuration values.

## Impact

- `Domain.AppUser` gains a nullable/optional bank IBAN field.
- `AppIdentityDbContext` needs a migration adding the bank IBAN column to the auth user table.
- Identity registration/profile UI needs to expose bank IBAN editing for the signed-in user.
- Monthly statement sending must load the current `AppUser` and pass bank account name/IBAN into the email payload.
- `BrevoEmailOptions` and startup validation should stop carrying bank account values.
- `.env.example` should remove or deprecate `BANK_ACCOUNT_NAME` and `BANK_IBAN`.
- Tests should cover email payload bank details and missing IBAN behavior.
