# invoice-document-upload-api Specification

## Purpose
Define the API endpoint that programs such as a Gmail forwarder use to upload invoice PDFs, and its rate limit.
## Requirements
### Requirement: Invoice document upload endpoint
The system SHALL provide `POST /api/v1/invoice-documents`, authenticated by API key, that accepts one PDF as the
multipart field `file` and runs the same validation, storage, duplicate detection and parse request as the UI upload.

#### Scenario: New PDF is accepted
- **WHEN** an authenticated request posts a valid PDF the user has not uploaded before
- **THEN** the system responds `202 Accepted` with the document id and status

#### Scenario: Duplicate PDF
- **WHEN** an authenticated request posts a PDF the user already uploaded
- **THEN** the system responds `200 OK` with the existing document id and status and sends no new parse request

#### Scenario: Invalid file
- **WHEN** an authenticated request posts no file, a non-PDF, an empty file or a file over 10 MB
- **THEN** the system responds `400 Bad Request` and records no document

#### Scenario: Unauthenticated request
- **WHEN** a request has no valid API key
- **THEN** the system responds `401 Unauthorized` and records no document

#### Scenario: Response exposes no internal types
- **WHEN** the endpoint responds
- **THEN** the body contains only the document id, status and file name, and no storage key or link

### Requirement: Upload rate limiting
The system SHALL limit API requests to 30 per minute per authenticated user with a named fixed-window policy that
every API controller uses, falling back to the client IP for unauthenticated requests.

#### Scenario: Limit exceeded
- **WHEN** a user sends more than 30 requests within one minute
- **THEN** further requests in that window receive `429 Too Many Requests` and record no document

#### Scenario: Users are limited separately
- **WHEN** one user is limited
- **THEN** requests with another user's key are not affected

