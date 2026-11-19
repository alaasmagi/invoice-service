# api-key-authentication Specification

## Purpose
Define how users create, revoke and use API keys that let other programs upload invoice PDFs on their behalf.
## Requirements
### Requirement: API key creation
The system SHALL let a signed-in user create a named API key, show the full key exactly once on creation, and store
only a hash of it together with a short display prefix.

#### Scenario: Key is shown once
- **WHEN** a user creates an API key
- **THEN** the system shows the full key once and afterwards shows only its name, prefix and dates

#### Scenario: Plain key is never stored
- **WHEN** an API key is created
- **THEN** the persisted record contains the hash and display prefix but not the full key

#### Scenario: Keys are recognisable
- **WHEN** an API key is generated
- **THEN** it starts with `isk_` and contains at least 32 random bytes

### Requirement: API key management
The system SHALL let a user list and revoke their own API keys, and SHALL hide other users' keys.

#### Scenario: User lists keys
- **WHEN** a user opens the API key settings page
- **THEN** the system lists that user's keys with name, prefix, creation time, last use and revoked state

#### Scenario: User revokes a key
- **WHEN** a user revokes one of their keys
- **THEN** requests using that key are rejected from then on

#### Scenario: Another user's key cannot be revoked
- **WHEN** a user tries to revoke a key they do not own
- **THEN** the system responds as if the key does not exist and changes nothing

### Requirement: API key authentication
The system SHALL authenticate API requests by the `X-Api-Key` header and act as the key's owner.

#### Scenario: Valid key
- **WHEN** a request carries a valid, unrevoked key
- **THEN** the request is authenticated as the key's owner and the key's last-use time is updated

#### Scenario: Missing, unknown or revoked key
- **WHEN** a request carries no key, an unknown key or a revoked key
- **THEN** the system responds with 401 and does not reveal which case applied

#### Scenario: API key does not open the web UI
- **WHEN** a request to a web UI page carries only an API key
- **THEN** the request is not authenticated for the web UI

