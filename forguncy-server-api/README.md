# Forguncy JWT login Web API

This class library targets Forguncy 8.0.4 and exposes one custom Web API route:

```text
POST /customapi/authapi/login
```

The API accepts either JSON or URL-encoded form data. It does not expose issue,
validate, refresh, logout, or any other external authentication route.

## Database setup

Run the minimal bootstrap script with a local MySQL client. The client prompts
for the database administrator password; do not put that password in this
repository or in the command history.

```powershell
mysql --host=<mysql-host> --user=<mysql-user> --password < .\sql\001-create-database.sql
```

The script creates the `forguncy_auth` database only. On the first API request,
Entity Framework Core `EnsureCreatedAsync` creates the `jwt_users` table from
the application model. Do not add a second SQL definition for that table.

## Configuration

Set these process or machine environment variables in the deployment
environment, secret manager, or Forguncy host configuration. Do not commit
their values, connection strings, signing keys, or bootstrap credentials.

```powershell
[Environment]::SetEnvironmentVariable('FGC_AUTH_MYSQL_CONNECTION', '<mysql-connection-string-from-secret-store>', 'User')
[Environment]::SetEnvironmentVariable('FGC_JWT_SIGNING_KEY', '<strong-random-secret-at-least-32-chars>', 'User')
[Environment]::SetEnvironmentVariable('FGC_JWT_ISSUER', '<issuer-name>', 'User')
[Environment]::SetEnvironmentVariable('FGC_JWT_EXPIRES_MINUTES', '<positive-integer-minutes>', 'User')
[Environment]::SetEnvironmentVariable('FGC_BOOTSTRAP_USERNAME', '<initial-username>', 'User')
[Environment]::SetEnvironmentVariable('FGC_BOOTSTRAP_PASSWORD', '<initial-password-from-secret-store>', 'User')
```

`FGC_AUTH_MYSQL_CONNECTION` and `FGC_JWT_SIGNING_KEY` are required. The
signing key must contain at least 32 characters. `FGC_JWT_ISSUER` defaults to
`forguncy-server-api`, and `FGC_JWT_EXPIRES_MINUTES` defaults to 60 minutes and
must be a positive integer when supplied. `FGC_BOOTSTRAP_USERNAME` and
`FGC_BOOTSTRAP_PASSWORD` are optional, but must be supplied together. If both
are set, the first initialization creates that enabled user when the username
does not already exist.

Restart the Forguncy application process after changing environment variables.

## Login contract

### JSON request

```http
Content-Type: application/json
```

```json
{
  "username": "<username>",
  "password": "<password>"
}
```

### Form request

```http
Content-Type: application/x-www-form-urlencoded
```

```text
username=<username>&password=<password>
```

Multipart form data is not accepted.

### Responses

Successful login returns `200 OK`:

```json
{
  "access_token": "<jwt>",
  "token_type": "Bearer",
  "expires_in": 3600,
  "user": {
    "id": 1,
    "username": "<username>"
  }
}
```

Malformed input, missing fields, or empty credentials return `400 Bad Request`:

```json
{
  "error": "invalid_request"
}
```

An unknown, disabled, or incorrectly authenticated user returns `401 Unauthorized`:

```json
{
  "error": "invalid_credentials"
}
```

Unexpected server or configuration failures return `500 Internal Server Error`
with the same non-sensitive shape every time:

```json
{
  "error": "server_error"
}
```

The `500` response does not expose exception details, configuration values,
database connection information, signing keys, or credentials.

## Test and release build

From `forguncy-server-api`, run:

```powershell
dotnet test .\ForguncyServerApi.sln
dotnet build .\ForguncyServerApi.csproj --configuration Release
```

The Release build produces:

```text
bin\Release\net6.0\ForguncyServerApi.dll
```

## Upload to Forguncy

In the Forguncy designer, use:

```text
File -> Settings -> Custom Web API -> Upload Web API Assembly
```

Upload `bin\Release\net6.0\ForguncyServerApi.dll`, then configure the
environment variables in the host that runs the Forguncy application. The
designer integration intentionally contains only the single login route;
issue and validate routes are intentionally absent by design.
