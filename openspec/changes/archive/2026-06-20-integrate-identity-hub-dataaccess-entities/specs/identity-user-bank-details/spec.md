## MODIFIED Requirements

### Requirement: Identity user stores bank IBAN
The system SHALL store the sender bank IBAN on the authenticated user's local invoice-service app profile.

#### Scenario: User has bank IBAN
- **WHEN** an authenticated user has filled a bank IBAN
- **THEN** the system SHALL persist the IBAN with that user's local app profile

#### Scenario: Different users have different bank IBANs
- **WHEN** two authenticated users fill different bank IBAN values
- **THEN** each user SHALL retain only their own bank IBAN

### Requirement: User can edit own bank IBAN
The system SHALL allow a signed-in user to view and update their own bank IBAN from the account/profile UI.

#### Scenario: User opens profile page
- **WHEN** a signed-in user opens their account/profile management page
- **THEN** the system SHALL show the current bank IBAN value for that user's local app profile

#### Scenario: User saves bank IBAN
- **WHEN** a signed-in user submits a valid bank IBAN on their account/profile page
- **THEN** the system SHALL save the IBAN to that user's local app profile

#### Scenario: User cannot edit another user's bank IBAN
- **WHEN** a signed-in user updates bank details
- **THEN** the system SHALL update only the authenticated user's local app profile

### Requirement: Bank account name comes from user fullname
The system SHALL use the authenticated sender's local invoice-service app profile full name as the bank account recipient name for monthly statement emails.

#### Scenario: Sender profile has fullname
- **WHEN** a monthly statement email is sent by a user whose local app profile has a full name
- **THEN** the payment recipient name SHALL be the sender's local app profile full name

#### Scenario: Sender profile has no fullname
- **WHEN** a monthly statement email is sent by a user whose local app profile has no full name
- **THEN** the system SHALL fail the send action with a clear profile setup error

### Requirement: Missing sender bank IBAN blocks monthly statement sending
The system SHALL NOT send a monthly statement email when the authenticated sender's local app profile has no bank IBAN configured.

#### Scenario: Sender has no bank IBAN
- **WHEN** a user without a bank IBAN attempts to send a monthly statement
- **THEN** the system SHALL not submit the email to Brevo
- **THEN** the system SHALL show a clear message that the sender bank IBAN must be configured first
