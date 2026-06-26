## ADDED Requirements

### Requirement: User role reads are authorized before auth-service delegation
The system SHALL authorize delegated user-role reads before calling the auth service for the target user's roles.

#### Scenario: Requesting user has no client roles
- **WHEN** a requesting user has no roles in this client scope
- **THEN** `GetUserRolesAsync` SHALL return a `Forbidden` failure
- **THEN** the service SHALL NOT call the auth service for the target user's roles

#### Scenario: Requesting user has a local role
- **WHEN** a requesting user's auth-service roles contain at least one role that exists in the local role table
- **THEN** `GetUserRolesAsync` SHALL call the auth service for the target user's roles
- **THEN** the service SHALL return a `UserRolesDto` containing the target user ID and roles

### Requirement: Role replacement validates local role names
The system SHALL validate every requested role name against the local role table before replacing a user's auth-service roles.

#### Scenario: Unknown role is requested
- **WHEN** `SetUserRolesAsync` receives a role name that does not match any local role case-insensitively
- **THEN** the service SHALL return `UnknownRole:{roleName}`
- **THEN** the service SHALL NOT call the auth service to replace roles

#### Scenario: Known roles are requested
- **WHEN** all requested role names match local roles case-insensitively
- **THEN** the service SHALL pass only validated role names to the auth-service client

### Requirement: Users cannot modify their own roles
The system SHALL reject delegated user-role modifications where the target user is the requesting user.

#### Scenario: User replaces own roles
- **WHEN** `SetUserRolesAsync` is called with equal target and requesting user IDs
- **THEN** the service SHALL return `CannotModifyOwnRoles`
- **THEN** the service SHALL NOT call the auth service to replace roles

#### Scenario: User removes own role
- **WHEN** `RemoveUserRoleAsync` is called with equal target and requesting user IDs
- **THEN** the service SHALL return `CannotModifyOwnRoles`
- **THEN** the service SHALL NOT call the auth service to remove the role

### Requirement: Role replacement enforces requester privilege
The system SHALL prevent a requesting user from assigning a role they do not hold or outrank.

#### Scenario: Requesting user has no client role
- **WHEN** a requesting user has no roles in this client scope
- **THEN** `SetUserRolesAsync` SHALL return `Forbidden`
- **THEN** the service SHALL NOT call the auth service

#### Scenario: Requesting user lacks sufficient privilege
- **WHEN** a requesting user attempts to assign a local role above their own privilege
- **THEN** `SetUserRolesAsync` SHALL return `InsufficientRole`
- **THEN** the service SHALL NOT call the auth service

#### Scenario: Requesting user has sufficient privilege
- **WHEN** a requesting user assigns only roles they hold or outrank
- **THEN** `SetUserRolesAsync` SHALL call the auth service to replace the target user's roles
- **THEN** the service SHALL return success when the auth service succeeds

### Requirement: Role removal validates local role names before delegation
The system SHALL validate the removed role name against the local role table before asking the auth service to remove it.

#### Scenario: Unknown role is removed
- **WHEN** `RemoveUserRoleAsync` receives a role name that does not match any local role case-insensitively
- **THEN** the service SHALL return `UnknownRole:{roleName}`
- **THEN** the service SHALL NOT call the auth service

#### Scenario: Known role is removed
- **WHEN** `RemoveUserRoleAsync` receives a known local role and the target differs from the requester
- **THEN** the service SHALL call the auth service to remove that role from the target user
- **THEN** the service SHALL return success when the auth service succeeds

### Requirement: Role management APIs map service results to documented HTTP responses
The system SHALL expose role-management API controllers that map Application result errors to stable HTTP status codes.

#### Scenario: Forbidden-style error is returned
- **WHEN** a controller receives `Forbidden`, `InsufficientRole`, or `CannotModifyOwnRoles`
- **THEN** the response SHALL be HTTP 403

#### Scenario: Not-found-style error is returned
- **WHEN** a controller receives `NotFound` or `UserNotInClient`
- **THEN** the response SHALL be HTTP 404

#### Scenario: Bad-request-style error is returned
- **WHEN** a controller receives `UnknownRole:*` or `CannotAssignAdminRole`
- **THEN** the response SHALL be HTTP 400

#### Scenario: Unauthorized error is returned
- **WHEN** a controller receives `Unauthorized`
- **THEN** the response SHALL be HTTP 401
