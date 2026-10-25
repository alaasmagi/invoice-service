## Why

This service must participate in centralized identity without exposing auth-service machine credentials to browsers or duplicating role state by hand. Local database roles need to remain the source of truth while the centralized auth service stays synchronized enough to issue correct role claims for this client.

## What Changes

- Reuse identity-hub configuration for auth-service base URL, client ID, and client secret.
- Add an Infrastructure HTTP client for machine-to-machine auth-service role sync and user role operations using `X-Client-Id` and `X-Client-Secret` headers.
- Add local role CRUD through an Application role service that automatically syncs the complete local role list to the auth service after successful create and update operations.
- Add delegated user role management through an Application service that enforces this service's authorization rules before calling the auth service.
- Add API controllers for local role management and delegated per-user role management, protected by JWT bearer authentication.
- Configure JWT bearer validation to trust tokens issued by the centralized identity service and read role claims from the `roles` claim.
- Preserve secret hygiene by ensuring `ClientSecret` is never logged or returned to clients.

## Capabilities

### New Capabilities

- `auth-service-client-integration`: Machine-to-machine integration with the centralized auth service using identity-hub client configuration for role synchronization and client-scoped user role operations.
- `local-role-management`: Local role CRUD with single-default-role enforcement and automatic non-fatal synchronization to the auth service.
- `delegated-user-role-management`: Admin-facing server-side user role management that validates local roles and enforces privilege rules before delegating to the auth service.
- `jwt-client-authentication`: JWT bearer validation for this service using the centralized identity service authority, audience, signing keys, and `roles` claim.

### Modified Capabilities

- `secret-hygiene`: Extend existing secret handling requirements to include the identity-hub client secret used for server-to-server auth-service calls.

## Impact

- Adds new files under `Infrastructure/AuthService`, `Application/Roles`, `Application/RoleManagement`, and `Web/Controllers`.
- Updates `Web/appsettings.json` and `Web/Program.cs` for auth-service options, typed HTTP client registration, JWT bearer authentication, and Application service DI.
- Requires the existing Domain `Role` entity and an existing role repository abstraction to be used as the local role source of truth.
- Introduces outbound HTTP dependency on the centralized auth service's client endpoints.
- Adds authenticated API surface for role administration and delegated user-role changes.
