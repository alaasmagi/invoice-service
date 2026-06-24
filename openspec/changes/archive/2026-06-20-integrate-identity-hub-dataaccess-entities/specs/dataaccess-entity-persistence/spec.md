## ADDED Requirements

### Requirement: AppDbContext maps DataAccess entities
The system SHALL use DataAccess entity classes as EF Core persistence models in `AppDbContext`.

#### Scenario: AppDbContext exposes persistence sets
- **WHEN** application persistence is configured
- **THEN** `AppDbContext` SHALL expose DbSets for DataAccess entity classes rather than domain entity classes

#### Scenario: EF relationships are configured on DataAccess entities
- **WHEN** EF Core builds the application model
- **THEN** indexes, relationships, precision, schema, concurrency, and delete behavior SHALL be configured against DataAccess entity classes

### Requirement: Domain models remain separate from persistence entities
The system SHALL keep domain models separate from EF Core persistence entities and SHALL use explicit mappers at repository boundaries.

#### Scenario: Repository reads data
- **WHEN** a repository loads records from `AppDbContext`
- **THEN** it SHALL map DataAccess entities to domain models before returning them to application or Web callers

#### Scenario: Repository writes data
- **WHEN** a repository persists a domain model
- **THEN** it SHALL map the domain model to a DataAccess entity before adding or updating the DbContext

### Requirement: App user profile is persisted in the application database
The system SHALL persist invoice-service user profile data through an `AppUserEntity` in the application database.

#### Scenario: Authenticated user signs in for the first time
- **WHEN** identity-hub authentication succeeds for a user that has no local app profile row
- **THEN** the system SHALL create an `AppUserEntity` whose id matches the authenticated `nameidentifier` claim

#### Scenario: Authenticated user already has a profile
- **WHEN** identity-hub authentication succeeds for a user that already has a local app profile row
- **THEN** the system SHALL reuse the existing `AppUserEntity` and SHALL NOT create a duplicate profile

#### Scenario: Profile stores invoice payment fields
- **WHEN** the local app user profile is persisted
- **THEN** it SHALL store the user's full name and optional bank IBAN for invoice-service workflows
