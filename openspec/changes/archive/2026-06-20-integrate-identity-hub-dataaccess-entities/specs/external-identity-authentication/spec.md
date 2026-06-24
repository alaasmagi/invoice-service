## ADDED Requirements

### Requirement: Razor login redirects to identity-hub
The system SHALL redirect browser login requests to identity-hub's Razor login page with the configured client id and a callback URL for invoice-service.

#### Scenario: User starts browser login
- **WHEN** an unauthenticated user starts login from invoice-service
- **THEN** the system SHALL redirect the browser to `{IdentityHub:BaseUrl}/Identity/Account/Login`
- **THEN** the redirect URL SHALL include `clientId` and `redirectUri` query parameters

#### Scenario: Redirect URL uses current callback origin
- **WHEN** the system builds the identity-hub login URL
- **THEN** `redirectUri` SHALL point to the invoice-service auth callback route
- **THEN** the callback origin SHALL be compatible with the identity-hub client's allowed origins

### Requirement: Razor callback exchanges code for claims
The system SHALL complete identity-hub browser login by exchanging callback auth codes for claims and signing in with a local invoice-service cookie.

#### Scenario: Callback contains auth code
- **WHEN** identity-hub redirects to the invoice-service callback with `code`
- **THEN** the system SHALL call identity-hub `/api/auth/external/token/exchange` with the code, client id, and client secret
- **THEN** the system SHALL create a local auth cookie from the returned claims
- **THEN** the system SHALL redirect the user to the requested local page or the home page

#### Scenario: Callback contains error
- **WHEN** identity-hub redirects to the invoice-service callback with `error`
- **THEN** the system SHALL NOT sign in the user
- **THEN** the system SHALL show a clear sign-in failure message

#### Scenario: Callback is missing code
- **WHEN** the invoice-service callback is requested without `code` and without `error`
- **THEN** the system SHALL NOT sign in the user
- **THEN** the system SHALL show a clear missing authentication code message

### Requirement: API authentication calls identity-hub
The system SHALL support invoice-service API authentication flows by calling identity-hub API endpoints and returning structured responses to the caller.

#### Scenario: API local login succeeds
- **WHEN** an API client submits email and password credentials to invoice-service
- **THEN** invoice-service SHALL call identity-hub `/api/auth/login` with the configured client id, response type, and callback URL
- **THEN** invoice-service SHALL exchange a returned auth code for claims before creating a local auth cookie or returning authenticated session information

#### Scenario: API external challenge is requested
- **WHEN** an API client requests an external provider challenge for Google or Microsoft
- **THEN** invoice-service SHALL call identity-hub `/api/auth/external/challenge`
- **THEN** invoice-service SHALL return the provider challenge redirect information to the caller

#### Scenario: API login requires additional identity-hub flow
- **WHEN** identity-hub returns `requiresTwoFactor` or `requiresConsent`
- **THEN** invoice-service SHALL return a structured response that identifies the required next step
- **THEN** invoice-service SHALL NOT treat the user as signed in until identity-hub returns an auth code that can be exchanged

#### Scenario: API identity-hub error occurs
- **WHEN** identity-hub returns `InvalidClient`, `InvalidRedirectUri`, `InvalidClientSecret`, `ConsentRequired`, or another auth error
- **THEN** invoice-service SHALL return a structured error response without exposing the client secret

### Requirement: Logout clears local invoice-service session
The system SHALL log users out by clearing the local invoice-service authentication cookie.

#### Scenario: User logs out
- **WHEN** a signed-in user submits logout
- **THEN** invoice-service SHALL clear the local auth cookie
- **THEN** the user SHALL no longer be authenticated in invoice-service

## MODIFIED Requirements

### Requirement: Configured external providers are available on account pages
The system SHALL NOT configure Google or Microsoft external sign-in providers locally in invoice-service. External provider choices SHALL be handled by identity-hub.

#### Scenario: Identity-hub login page offers providers
- **WHEN** a user is redirected from invoice-service to identity-hub login
- **THEN** provider options SHALL be determined by identity-hub configuration

#### Scenario: invoice-service has no provider secrets
- **WHEN** invoice-service starts
- **THEN** it SHALL NOT require Google or Microsoft client id or client secret configuration for authentication

### Requirement: External login creates a local Identity user
The system SHALL complete a successful first-time Google or Microsoft login by creating a local invoice-service app user profile associated with the identity-hub user id.

#### Scenario: First-time external login
- **WHEN** a user successfully authenticates through identity-hub and returns to invoice-service with exchanged claims
- **THEN** the system SHALL create an `AppUser` profile record when one does not already exist
- **THEN** the system SHALL sign the user in with a local invoice-service auth cookie
- **THEN** the system SHALL NOT create local credentials or local external provider login records

#### Scenario: Provider does not return required account information
- **WHEN** identity-hub returns claims without optional profile information
- **THEN** invoice-service SHALL still sign in the user if the required user id claim is present
- **THEN** invoice-service SHALL allow the user to complete invoice-service profile fields locally

### Requirement: External login reuses linked Identity users
The system SHALL sign in returning identity-hub users through the existing local invoice-service app user profile that matches the identity-hub user id.

#### Scenario: Returning external user
- **WHEN** a user authenticates through identity-hub and the returned user id matches an existing local app user profile
- **THEN** the system SHALL sign in the user without creating a duplicate profile

### Requirement: External authentication preserves user data isolation
The system MUST use the identity-hub user id claim from the signed-in local auth cookie for all existing ownership scoping.

#### Scenario: External user accesses business data
- **WHEN** a user signs in through identity-hub and opens any user-scoped invoice-service page
- **THEN** the system only shows and mutates records owned by that signed-in user id

### Requirement: Local Identity authentication remains available
The system SHALL remove local ASP.NET Core Identity registration, login, external provider, and password authentication from invoice-service. Users SHALL authenticate through identity-hub.

#### Scenario: User chooses login
- **WHEN** an unauthenticated user chooses login in invoice-service
- **THEN** invoice-service SHALL start the identity-hub login flow

#### Scenario: User needs account registration or password management
- **WHEN** a user needs registration, password, consent, two-factor, or provider account management
- **THEN** those flows SHALL be handled by identity-hub rather than local invoice-service Identity pages
