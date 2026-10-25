## ADDED Requirements

### Requirement: JWT bearer authentication validates identity tokens
The system SHALL configure JWT bearer authentication for API endpoints using the centralized identity service authority and client audience.

#### Scenario: JWT bearer options are configured
- **WHEN** the application starts
- **THEN** JWT validation SHALL use the identity-hub base URL as the bearer authority
- **THEN** JWT validation SHALL require the audience to match the identity-hub client ID
- **THEN** JWT validation SHALL validate issuer signing keys from the identity service metadata

#### Scenario: Role claims are evaluated
- **WHEN** a valid JWT contains a `roles` claim
- **THEN** ASP.NET Core authorization SHALL treat `roles` values as role claims for `User.IsInRole` and role-based authorization

### Requirement: Role management controllers require bearer authentication
The system SHALL require JWT bearer authentication on the local role and delegated user-role API controllers.

#### Scenario: Unauthenticated request reaches role API
- **WHEN** a request without a valid bearer token calls `api/roles` or `api/users/{userId}/roles`
- **THEN** the response SHALL be HTTP 401

#### Scenario: Authenticated request reaches role API
- **WHEN** a request with a valid bearer token calls a role-management endpoint
- **THEN** the controller action SHALL execute using the authenticated principal

### Requirement: Requesting user ID is read from JWT claims
The system SHALL derive the requesting user ID for delegated user-role APIs from `ClaimTypes.NameIdentifier`.

#### Scenario: Name identifier claim is missing or invalid
- **WHEN** a delegated user-role API request lacks a parseable `ClaimTypes.NameIdentifier` GUID
- **THEN** the controller SHALL return HTTP 401

#### Scenario: Name identifier claim is valid
- **WHEN** a delegated user-role API request contains a parseable `ClaimTypes.NameIdentifier` GUID
- **THEN** the controller SHALL pass that GUID as the requesting user ID to the Application service
