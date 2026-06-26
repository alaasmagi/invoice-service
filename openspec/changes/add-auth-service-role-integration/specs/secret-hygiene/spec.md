## ADDED Requirements

### Requirement: Auth service secrets are not exposed
The system SHALL treat the identity-hub client secret used for auth-service calls as a secret-bearing configuration value.

#### Scenario: Auth service credentials are logged
- **WHEN** auth-service options, request headers, authentication configuration, or exceptions are logged
- **THEN** logs SHALL NOT include `ClientSecret`

#### Scenario: Auth service configuration is returned to clients
- **WHEN** any controller response is serialized
- **THEN** the response SHALL NOT include `ClientSecret`

#### Scenario: Auth service secret placeholders are not duplicated
- **WHEN** secret-bearing environment documentation is updated for auth-service integration
- **THEN** it SHALL reuse the identity-hub client secret placeholder instead of documenting a second auth-service secret
