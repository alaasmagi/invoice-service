## ADDED Requirements

### Requirement: All business entities carry AppUserId
Every DataAccess DTO entity SHALL have an `AppUserId` property (inherited from `BaseEntityUserWithMetaConcurrency`) that stores the authenticated Identity user's ID. This links all business data to its owner.

#### Scenario: New entity receives the current user's ID
- **WHEN** a new business entity is created through any service or controller
- **THEN** its `AppUserId` SHALL be set to the authenticated user's `IdentityUser.Id`

### Requirement: Global query filter scopes reads to the current user
`AppDbContext` SHALL apply a global query filter on every business entity so that queries automatically exclude records belonging to other users.

#### Scenario: User queries addresses
- **WHEN** User A queries addresses
- **THEN** only addresses where `AppUserId` matches User A's ID SHALL be returned

#### Scenario: User cannot see another user's data
- **WHEN** User A queries any entity type
- **THEN** records where `AppUserId` belongs to User B SHALL NOT appear in the results

### Requirement: Write operations enforce ownership
All update and delete operations SHALL verify that the target entity's `AppUserId` matches the authenticated user before proceeding.

#### Scenario: User attempts to update another user's entity
- **WHEN** User A attempts to update an entity owned by User B
- **THEN** the operation SHALL fail or return not found

#### Scenario: User attempts to delete another user's entity
- **WHEN** User A attempts to delete an entity owned by User B
- **THEN** the operation SHALL fail or return not found

### Requirement: Controllers extract user ID from claims
Web controllers SHALL extract the current user's ID from `User.FindFirstValue(ClaimTypes.NameIdentifier)` and pass it to services or set it on entities before persistence.

#### Scenario: Controller sets AppUserId on create
- **WHEN** a controller handles a create request
- **THEN** it SHALL set `AppUserId` from the authenticated user's claim before calling the service

### Requirement: Unauthenticated access is denied
All business entity controllers SHALL require authentication. Unauthenticated requests SHALL be redirected to the login page.

#### Scenario: Anonymous user accesses a business page
- **WHEN** an unauthenticated user navigates to any business entity page
- **THEN** they SHALL be redirected to the Identity login page

