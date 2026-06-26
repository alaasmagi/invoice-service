## ADDED Requirements

### Requirement: Auth service options are available as typed configuration
The system SHALL provide typed auth-service configuration derived from the existing identity-hub base URL, client ID, and client secret.

#### Scenario: Auth service options are registered
- **WHEN** the application starts
- **THEN** `AuthServiceOptions` SHALL be populated from the resolved identity-hub configuration
- **THEN** the configured client ID SHALL be parsed as a `Guid`

#### Scenario: Auth service settings reuse identity-hub configuration
- **WHEN** environment documentation is inspected
- **THEN** it SHALL NOT require separate `AuthService__*` values when identity-hub settings are already configured

### Requirement: Machine-to-machine auth service calls use client credentials
The system SHALL send `X-Client-Id` and `X-Client-Secret` headers on every auth-service machine-to-machine request.

#### Scenario: Role sync request is sent
- **WHEN** roles are synchronized to the auth service
- **THEN** the outgoing request SHALL include `X-Client-Id` with the configured client ID
- **THEN** the outgoing request SHALL include `X-Client-Secret` with the configured client secret

#### Scenario: User role request is sent
- **WHEN** user roles are read, replaced, or removed through the auth-service client
- **THEN** the outgoing request SHALL include the configured client credential headers

### Requirement: Auth service client wraps role sync endpoint
The system SHALL provide an `IAuthServiceClient.SyncRolesAsync` method that posts the complete supplied role definitions to the auth-service role sync endpoint.

#### Scenario: Roles are synchronized
- **WHEN** `SyncRolesAsync` is called with role definitions
- **THEN** the client SHALL send `POST api/client/roles/sync`
- **THEN** the JSON body SHALL contain `roles` entries with `name` and `isDefault`

### Requirement: Auth service client wraps user role endpoints
The system SHALL provide methods to get, replace, and remove client-scoped user roles through the auth service.

#### Scenario: User roles are read
- **WHEN** `GetUserRolesAsync` is called for a user ID
- **THEN** the client SHALL send `GET api/client/users/{userId}/roles`
- **THEN** a successful response SHALL return the response roles as a read-only list

#### Scenario: Missing user role scope is read
- **WHEN** the auth service returns 404 for `GetUserRolesAsync`
- **THEN** the client SHALL return an empty role list
- **THEN** the client SHALL NOT throw an exception

#### Scenario: User roles are replaced
- **WHEN** `SetUserRolesAsync` is called with role names
- **THEN** the client SHALL send `POST api/client/users/{userId}/roles`
- **THEN** the JSON body SHALL contain the full replacement role list

#### Scenario: User role is removed
- **WHEN** `RemoveUserRoleAsync` is called with a role name containing route-sensitive characters
- **THEN** the role name embedded in `DELETE api/client/users/{userId}/roles/{roleName}` SHALL be URL-encoded

### Requirement: Auth service failures use typed exceptions
The system SHALL throw `AuthServiceException` for non-success auth-service responses except the 404 user-role read case.

#### Scenario: Auth service returns an error body
- **WHEN** the auth service returns a non-success status with a JSON body containing `error`
- **THEN** the thrown `AuthServiceException` SHALL expose the HTTP status code
- **THEN** the thrown `AuthServiceException` SHALL expose the parsed error code

#### Scenario: Auth service returns a non-JSON error body
- **WHEN** the auth service returns a non-success status without parseable JSON error content
- **THEN** the thrown `AuthServiceException` SHALL expose the HTTP status code
- **THEN** the thrown `AuthServiceException` SHALL have no error code
