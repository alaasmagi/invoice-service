## 1. Configuration and Auth Service Infrastructure

- [x] 1.1 Verify the current role domain type and repository shape; use the existing `Role` entity if present, or reconcile the existing `AppRole` naming without creating a second role source of truth.
- [x] 1.2 Add `AuthServiceOptions` under `Infrastructure/AuthService` with `BaseUrl`, `ClientId`, and `ClientSecret`.
- [x] 1.3 Reuse existing identity-hub settings instead of adding a duplicate `AuthService` config section.
- [x] 1.4 Register `AuthServiceOptions` in `Web/Program.cs` from resolved identity-hub configuration.
- [x] 1.5 Add `AuthServiceException` with `StatusCode` and optional `ErrorCode`.
- [x] 1.6 Add `RoleSyncDefinition` with role name and default-role flag.
- [x] 1.7 Add `IAuthServiceClient` with sync, get user roles, set user roles, and remove user role methods.
- [x] 1.8 Implement `AuthServiceClient` as a typed `HttpClient` client that adds client credential headers through a private helper on every request.
- [x] 1.9 Implement non-success response handling in `AuthServiceClient`, including parsed `{ "error": "..." }` bodies and empty-list behavior for 404 user-role reads.
- [x] 1.10 Ensure `RemoveUserRoleAsync` URL-encodes the role name before embedding it in the route.
- [x] 1.11 Register `IAuthServiceClient` with `AddHttpClient` and the configured auth-service base address.

## 2. JWT Authentication

- [x] 2.1 Add required JWT bearer package references if the Web project does not already include them transitively.
- [x] 2.2 Configure JWT bearer authentication in `Web/Program.cs` using the identity-hub base URL as authority and identity-hub client ID as audience.
- [x] 2.3 Set JWT `RoleClaimType` to `roles` so role authorization uses auth-service role claims.
- [x] 2.4 Preserve existing cookie authentication behavior for current MVC flows while requiring bearer authentication explicitly on the new API controllers.

## 3. Local Role Application Service

- [x] 3.1 Add `CreateRoleRequest` and `UpdateRoleRequest` records under `Application/Roles/Requests`.
- [x] 3.2 Add FluentValidation validators for create and update requests requiring non-empty, non-whitespace names with maximum length 100.
- [x] 3.3 Add `IRoleService` with get all, create, update, and delete methods returning the project's `Result` type.
- [x] 3.4 Implement `RoleService.GetAllAsync` using the existing role repository abstraction.
- [x] 3.5 Implement `RoleService.CreateAsync` with case-insensitive duplicate-name validation, single-default-role enforcement, local save, and complete-list auth-service sync.
- [x] 3.6 Implement `RoleService.UpdateAsync` with missing-role handling, default-role transition handling, local save, and complete-list auth-service sync.
- [x] 3.7 Implement `RoleService.DeleteAsync` with missing-role handling and local deletion only; do not call auth-service sync.
- [x] 3.8 Add `TrySyncWithAuthServiceAsync` that fetches all local roles, maps them to `RoleSyncDefinition`, logs successful sync, logs `AuthServiceException` status without secrets, and logs unexpected failures.
- [x] 3.9 Register `IRoleService` as scoped in `Web/Program.cs`.

## 4. Delegated User Role Management Service

- [x] 4.1 Add `UserRolesDto` under `Application/RoleManagement/Dtos`.
- [x] 4.2 Add `IUserRoleManagementService` with methods for reading, replacing, and removing a target user's roles.
- [x] 4.3 Implement requester role lookup through `IAuthServiceClient.GetUserRolesAsync(requestingUserId)` and local role matching with case-insensitive comparisons.
- [x] 4.4 Implement delegated user-role read authorization so requesters with no valid local client role receive `Forbidden` before target-role lookup.
- [x] 4.5 Implement full role replacement self-modification rejection with `CannotModifyOwnRoles`.
- [x] 4.6 Implement full role replacement validation so every requested role exists locally or returns `UnknownRole:{roleName}` before auth-service delegation.
- [x] 4.7 Implement full role replacement privilege checks using the default role as lowest privilege and non-default roles as higher privilege unless an existing persisted ordering model is found.
- [x] 4.8 Implement role removal self-modification rejection and local role-name validation before auth-service delegation.
- [x] 4.9 Register `IUserRoleManagementService` as scoped in `Web/Program.cs`.

## 5. API Controllers

- [x] 5.1 Add `RolesController` under `Web/Controllers` at `api/roles` with controller-level JWT bearer authorization.
- [x] 5.2 Implement `GET api/roles` returning local roles.
- [x] 5.3 Implement `POST api/roles` returning 201 on success and mapped error responses on failure.
- [x] 5.4 Implement `PUT api/roles/{id:guid}` using the route ID with the update request body and mapped error responses.
- [x] 5.5 Implement `DELETE api/roles/{id:guid}` returning 200 on success and 404 for missing roles.
- [x] 5.6 Add `UserRolesController` under `Web/Controllers` at `api/users/{userId:guid}/roles` with controller-level JWT bearer authorization.
- [x] 5.7 Implement `GET api/users/{userId:guid}/roles`, deriving requesting user ID from `ClaimTypes.NameIdentifier`.
- [x] 5.8 Implement `POST api/users/{userId:guid}/roles` for full replacement using request body `{ roles: string[] }`.
- [x] 5.9 Implement `DELETE api/users/{userId:guid}/roles/{roleName}` for delegated role removal.
- [x] 5.10 Add shared controller result mapping for `Forbidden`, `InsufficientRole`, `CannotModifyOwnRoles`, `NotFound`, `UserNotInClient`, `UnknownRole:*`, `CannotAssignAdminRole`, and `Unauthorized`.
- [x] 5.11 Add XML Swagger documentation and `[ProducesResponseType]` attributes to every action in both controllers.

## 6. Verification

- [ ] 6.1 Add or update unit tests for `AuthServiceClient` request routes, headers, 404 empty-list behavior, error parsing, and role-name URL encoding.
- [ ] 6.2 Add or update unit tests for `RoleService` duplicate validation, default-role clearing, complete-list sync after create/update, non-fatal sync failures, and no sync after delete.
- [ ] 6.3 Add or update unit tests for `UserRoleManagementService` forbidden access, unknown roles, self-modification rejection, privilege checks, and successful delegation.
- [ ] 6.4 Add or update controller tests or integration checks for JWT authorization, requesting-user claim parsing, and result-to-HTTP mapping.
- [ ] 6.5 Run `dotnet build` for the solution and fix compile or nullable warnings introduced by the change.
- [ ] 6.6 Run the relevant test suite and verify no secrets appear in logs, responses, snapshots, or tracked config files.
