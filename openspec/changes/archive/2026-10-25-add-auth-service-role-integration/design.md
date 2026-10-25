## Context

The service already relies on a centralized identity provider for sign-in flows and stores application data in its own database. Role records now need to be managed locally as domain data while the centralized auth service remains the system that issues role claims in JWTs for this client.

The change crosses Infrastructure, Application, and Web. Infrastructure owns machine-to-machine HTTP calls. Application owns local role orchestration and delegated user-role authorization. Web exposes authenticated API endpoints and configures JWT bearer validation for tokens issued by the auth service.

The existing browser login path uses cookie authentication and an `IdentityHub` client. The new role APIs should add JWT bearer authentication for API callers without requiring a broad rewrite of the current cookie-based MVC flow.

## Goals / Non-Goals

**Goals:**

- Reuse the existing identity-hub base URL, client ID, and client secret for auth-service calls.
- Add a typed HTTP client that wraps auth-service role sync and client-scoped user role endpoints.
- Keep local `Role` records as the source of truth for available roles.
- Automatically sync the complete local role list after successful role create and update operations.
- Treat auth-service sync failures after local DB writes as non-fatal.
- Add server-side delegated user-role management so browsers never call the auth service directly for role changes.
- Protect new role-management controllers with JWT bearer authentication and map service errors to stable HTTP statuses.
- Preserve secret hygiene for machine credentials.

**Non-Goals:**

- Do not change auth-service behavior or endpoints.
- Do not introduce hardcoded role-name constants as the role source of truth.
- Do not delete roles from the auth service when local roles are deleted.
- Do not redesign the existing MVC cookie login flow unless needed to coexist with JWT bearer auth.
- Do not merge application and identity database contexts.

## Decisions

1. Use a typed `IAuthServiceClient` registered with `AddHttpClient`.

   Rationale: this keeps the auth-service protocol centralized, testable, and compatible with `IHttpClientFactory` lifetime management. The client adds `X-Client-Id` and `X-Client-Secret` per request through one helper so credential handling is consistent.

   Alternative considered: construct `HttpClient` manually in services. Rejected because it duplicates transport setup, complicates testing, and bypasses the standard factory.

2. Represent auth-service failures with `AuthServiceException`.

   Rationale: callers need the HTTP status and optional auth-service error code to distinguish expected machine-to-machine failures from other exceptions. The exception must not include secrets.

   Alternative considered: return `Result<T>` from the HTTP client. Rejected because transport failures are exceptional at the integration boundary and the requested service behavior specifically catches `AuthServiceException`.

3. Keep sync as a side effect of successful local role writes.

   Rationale: the local database remains authoritative and admin workflows stay simple. Syncing the complete local role list ensures the auth service can update its default role reference from the current local `IsDefault` flags.

   Alternative considered: add a manual sync endpoint. Rejected because it creates an extra operational step and makes local role writes easier to forget.

4. Make sync failures non-fatal after local writes.

   Rationale: the local write has already succeeded, and the auth service skips existing roles while updating default role state on every later successful sync. Logging the failure gives operators visibility without rolling back local role changes.

   Alternative considered: fail or roll back local writes when sync fails. Rejected because this couples local availability to an external service and conflicts with the requested reconciliation behavior.

5. Enforce delegated role-management authorization in Application services before outbound auth-service calls.

   Rationale: the browser must never hold auth-service machine credentials, and this client owns the rules for which local admins can manage client-scoped roles. The service validates requested role names against local `Role` records before delegation.

   Alternative considered: expose auth-service role endpoints directly to browser clients. Rejected because it leaks authority outside this service and bypasses local business rules.

6. Add JWT bearer authentication alongside the existing cookie scheme.

   Rationale: new API controllers require bearer tokens from identity hub while existing MVC pages may continue to use cookies. Controller-level `[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]` keeps the new API surface explicit. Using JWT bearer `Authority` lets the service validate issuer signing keys from identity metadata instead of requiring a duplicated shared signing secret.

   Alternative considered: switch the whole application default scheme to JWT. Rejected because it risks breaking existing MVC flows that currently use cookies.

## Risks / Trade-offs

- Role repository naming or entity mismatch → Confirm the current repository and domain type before implementation. If the project still uses `AppRole`, reconcile it intentionally with the requested `Role` source of truth instead of introducing a second role model.
- Auth service unavailable during local writes → Log non-fatal sync failures and rely on the next successful create/update sync to reconcile auth-service state.
- Role privilege ordering is underspecified → Treat the default role as lowest privilege and all non-default roles as higher privilege unless implementation discovers a persisted ordering model. If stricter hierarchy is needed later, add an explicit ordering field in a separate change.
- Existing cookie and new JWT authentication can interact unexpectedly → Keep bearer auth explicit on new controllers and preserve existing cookie configuration for MVC routes.
- Secret values can leak through logs or config diagnostics → Never log option objects, request headers, or response payloads containing configured secrets.
