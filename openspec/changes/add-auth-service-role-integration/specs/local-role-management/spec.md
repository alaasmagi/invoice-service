## ADDED Requirements

### Requirement: Local roles are managed through an application service
The system SHALL provide role CRUD operations through `IRoleService` using the local `Role` repository as the source of truth.

#### Scenario: Roles are listed
- **WHEN** `GetAllAsync` is called
- **THEN** the service SHALL return all local role records

#### Scenario: Role name is invalid
- **WHEN** a create or update request has a null, empty, whitespace-only, or over-100-character name
- **THEN** validation SHALL fail before persisting the role

#### Scenario: Role name already exists
- **WHEN** a create request uses a name that already exists in the local role table with different casing
- **THEN** the service SHALL return a failure result
- **THEN** the service SHALL NOT create a duplicate local role

### Requirement: Only one local role can be default
The system SHALL ensure at most one local role has `IsDefault` set to true after role create or update operations.

#### Scenario: Default role is created
- **WHEN** a new role is created with `IsDefault` set to true
- **THEN** all other local roles SHALL have `IsDefault` cleared before the operation completes

#### Scenario: Existing role becomes default
- **WHEN** an existing non-default role is updated with `IsDefault` set to true
- **THEN** all other local roles SHALL have `IsDefault` cleared before the operation completes

### Requirement: Successful role create and update synchronize all local roles
The system SHALL synchronize the complete current local role list to the auth service after successful local role create and update operations.

#### Scenario: Role is created
- **WHEN** a role is successfully saved to the local database
- **THEN** the service SHALL fetch the complete current local role list
- **THEN** the service SHALL call auth-service role sync with every local role mapped to name and default flag

#### Scenario: Role is updated
- **WHEN** a role update is successfully saved to the local database
- **THEN** the service SHALL fetch the complete current local role list
- **THEN** the service SHALL call auth-service role sync with every local role mapped to name and default flag

### Requirement: Role sync failures after local writes are non-fatal
The system SHALL preserve successful local role writes when auth-service synchronization fails.

#### Scenario: Auth service sync returns a typed failure
- **WHEN** a local create or update succeeds and auth-service sync throws `AuthServiceException`
- **THEN** the service SHALL log the failure without logging configured secrets
- **THEN** the service SHALL return a successful result for the local write

#### Scenario: Auth service sync throws an unexpected exception
- **WHEN** a local create or update succeeds and sync throws an unexpected exception
- **THEN** the service SHALL log the unexpected failure without logging configured secrets
- **THEN** the service SHALL return a successful result for the local write

### Requirement: Role delete does not synchronize with auth service
The system SHALL delete local role records without triggering auth-service role sync.

#### Scenario: Role is deleted
- **WHEN** `DeleteAsync` deletes an existing local role
- **THEN** the service SHALL NOT call auth-service role sync
- **THEN** the service SHALL return a successful result

#### Scenario: Missing role is deleted
- **WHEN** `DeleteAsync` is called for a role ID that does not exist
- **THEN** the service SHALL return a `NotFound` failure
