# Forguncy JWT login Web API

This class library targets Forguncy 8.0.4 and exposes one custom Web API route:

```text
POST /customapi/authapi/login
```

The API accepts either JSON or URL-encoded form data. It does not expose issue,
validate, refresh, logout, or any other external authentication route.

## Existing database contract

Forguncy supplies the existing database and `c_userinfo` table. This API only
reads matching user records; it does not create or alter schemas, seed users,
or initialize database content. Do not run database bootstrap SQL for this API.

The login request field `username` means the enterprise credit code stored in
`c_userinfo.creditCode`. The existing `c_userinfo.password` value must use the
lowercase middle 16 characters of the password's MD5 digest:

```text
lowercaseHex(MD5(UTF8(password))).Substring(8, 16)
```

A matching user can log in only when `c_userinfo.isopen` equals the integer
`1`.

## Configuration

Set these process or machine environment variables in the deployment
environment, secret manager, or Forguncy host configuration. Do not commit
their values or signing keys.

```powershell
[Environment]::SetEnvironmentVariable('FGC_JWT_SIGNING_KEY', '<strong-random-secret-at-least-32-chars>', 'User')
[Environment]::SetEnvironmentVariable('FGC_JWT_ISSUER', '<issuer-name>', 'User')
[Environment]::SetEnvironmentVariable('FGC_JWT_EXPIRES_MINUTES', '<positive-integer-minutes>', 'User')
```

`FGC_JWT_SIGNING_KEY` is required and must contain at least 32 characters.
`FGC_JWT_ISSUER` defaults to `forguncy-server-api`, and
`FGC_JWT_EXPIRES_MINUTES` defaults to 60 minutes and must be a positive integer
when supplied.

The MySQL connection is not configured with
`FGC_AUTH_MYSQL_CONNECTION`. The existing database connection is selected by
`config.item='ssl'`: set its connection string in that row's `value` column.
The Forguncy 8.0.4 SDK `IDataAccess` reads that row, and the application uses
its `value` as the EF Core/MySQL connection string. The lookup intentionally
does not filter on `enable`. Keep the connection string and its credentials out
of environment variables, documentation, diagnostics, and logs.

Restart the Forguncy application process after changing environment variables.

Expose the login route only through the Forguncy site's HTTPS endpoint or an
equivalent trusted network boundary. Never expose credentials through an
unprotected direct HTTP endpoint.

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
dotnet test .\ForguncyServerApi.sln --configuration Release --no-restore -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
dotnet build .\ForguncyServerApi.csproj --configuration Release --no-restore -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
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

Upload `bin\Release\net6.0\ForguncyServerApi.dll`, then configure the JWT and
environment variables in the host that runs the Forguncy application.
Configure the existing database connection only through the Forguncy `config`
table row where `item='ssl'`; the API does not initialize that database. The
designer integration intentionally contains only the single login route; issue
and validate routes are intentionally absent by design.
