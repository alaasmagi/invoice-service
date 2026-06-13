## Requirements

### Requirement: Configured external providers are available on account pages
The system SHALL offer Google and Microsoft external sign-in options on Identity account pages when the corresponding provider has both a client ID and client secret configured.

#### Scenario: Google configuration is complete
- **WHEN** a user opens the login page and Google authentication has a configured client ID and client secret
- **THEN** the system displays a Google sign-in option

#### Scenario: Microsoft configuration is complete
- **WHEN** a user opens the login page and Microsoft authentication has a configured client ID and client secret
- **THEN** the system displays a Microsoft sign-in option

#### Scenario: Provider configuration is incomplete
- **WHEN** a user opens the login page and a provider is missing either its client ID or client secret
- **THEN** the system does not display a sign-in option for that provider

### Requirement: External login creates a local Identity user
The system SHALL complete a successful first-time Google or Microsoft login by creating an `AppUser` in ASP.NET Core Identity and associating the external provider login with that user.

#### Scenario: First-time external login
- **WHEN** a user successfully authenticates with a configured external provider and confirms the required local account information
- **THEN** the system creates an `AppUser`, stores the external provider login association, signs the user in, and redirects to the requested local return URL

#### Scenario: Provider does not return required account information
- **WHEN** a user successfully authenticates with a configured external provider but the provider response does not include required local account information
- **THEN** the system requires the missing information before creating the `AppUser`

### Requirement: External login reuses linked Identity users
The system SHALL sign in returning external users through the existing `AppUser` associated with the external provider login.

#### Scenario: Returning external user
- **WHEN** a user authenticates with a configured external provider that is already linked to an `AppUser`
- **THEN** the system signs in the linked `AppUser` without creating a duplicate local account

### Requirement: External authentication preserves user data isolation
The system MUST use the local Identity user ID from the signed-in `AppUser` for all existing ownership scoping after Google or Microsoft authentication.

#### Scenario: External user accesses business data
- **WHEN** a user signs in with Google or Microsoft and opens any user-scoped invoice service page
- **THEN** the system only shows and mutates records owned by that signed-in `AppUser`

### Requirement: Local Identity authentication remains available
The system SHALL preserve existing local registration, login, logout, and account management behavior when Google or Microsoft authentication is added.

#### Scenario: No external providers configured
- **WHEN** neither Google nor Microsoft authentication is configured
- **THEN** users can still register, log in, log out, and manage their account with local Identity credentials

#### Scenario: User chooses local login
- **WHEN** external providers are configured and a user logs in with local Identity credentials
- **THEN** the system signs the user in through the existing local Identity flow
