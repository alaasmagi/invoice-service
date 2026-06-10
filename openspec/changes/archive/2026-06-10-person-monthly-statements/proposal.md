## Why

The current monthly statement model is address/month centered, but the user-facing statement should be person/month centered: one person receives one unified statement that includes every address they are related to and every invoice share they owe for that month. This better matches the payment and email workflow, where the recipient wants one monthly total with a clear address/service invoice breakdown.

## What Changes

- **BREAKING**: Redefine `MonthlyStatement` as one contact/person for one calendar month, not one address for one calendar month.
- A monthly statement SHALL contain the person's name/contact, the year/month, total amount due, status, sent metadata, and all invoice shares for that person across all relevant addresses.
- Address information moves into statement line details: each line represents one invoice share for one address/service/invoice.
- Generation will build one statement per active person for the selected month across all addresses they are related to.
- If multiple active people live at the same address during an invoice month, that invoice amount is split evenly among those people and each share is added to that person's statement.
- A person related to multiple addresses receives one unified statement that includes all relevant addresses and invoices.
- The monthly statement detail UI and email payload will show person name, all addresses, invoice service names, invoice totals, split shares, and person total.
- Existing address/month statement screens and send logic will be adapted to the person/month model.

## Capabilities

### New Capabilities
- `person-monthly-statements`: Defines person/month statement generation, invoice splitting, statement contents, and send behavior.

### Modified Capabilities
- `brevo-email-delivery`: Monthly statement email content must include the unified person/month statement with address and service invoice breakdown.

## Impact

- **Domain**: `MonthlyStatement` will reference a contact/person instead of a single address; statement lines may need a new domain type or reuse/enrich `ContactMonthlyStatement`/invoice allocation concepts.
- **DTO/DataAccess**: Persistence entities, relationships, indexes, and migrations must change to support person/month statements and line-level invoice details.
- **Application**: `GenerateMonthlyStatementService` must aggregate by person across all active address relationships and split address invoices evenly among active residents.
- **Application/Web email**: `SendMonthlyStatementService`, `IEmailSender`, and Brevo email rendering must send one unified email per person/month.
- **Web**: Monthly statement generate/details/index views and controller logic must display person-centered statements with address/service/invoice lines.
- **Migrations/Data**: Existing address/month statement data may need migration, discard, or regeneration because the statement identity changes.
