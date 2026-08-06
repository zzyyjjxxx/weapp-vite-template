# Forguncy enterprise auth Web API

This class library targets Forguncy 8.0.4 and exposes three custom Web API routes:

```text
POST /customapi/enterpriseapi/login
POST /customapi/enterpriseapi/refresh
GET /customapi/enterpriseapi/getinfo
```

Both routes accept either JSON or URL-encoded form data. The API does not
expose issue, validate, logout, or any other external authentication route.

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

JWT parameters are loaded from the existing Forguncy `config` table by the
following `item` values. If a row is missing, or its `value` is blank, the API
generates or fills the value and persists it before serving login requests.

| `item` | Meaning | Missing or blank `value` |
| --- | --- | --- |
| `FGC_JWT_SIGNING_KEY` | HMAC signing key | Generates 32 cryptographically random bytes and stores them as Base64. |
| `FGC_JWT_ISSUER` | JWT issuer | Generates `forguncy-server-api-` plus a new 32-character lowercase GUID. |
| `FGC_JWT_EXPIRES_MINUTES` | Token lifetime in minutes | Stores `60`. |
| `FGC_JWT_REFRESH_EXPIRES_MINUTES` | Refresh-token lifetime in minutes | Stores `10080`. |

Existing non-blank values are used as-is. The signing key must contain at least
32 characters and the expiration values must be positive integers. Invalid
non-blank values fail initialization instead of being overwritten. Do not put
JWT values in environment variables or commit them to source control.

The MySQL connection is not configured with
`FGC_AUTH_MYSQL_CONNECTION`. The existing database connection is selected by
`config.item='ssl'`: set its connection string in that row's `value` column.
The Forguncy 8.0.4 SDK `IDataAccess` reads that row, and the application uses
its `value` as the SqlSugar/MySQL connection string. The lookup intentionally
does not filter on `enable`. Keep the connection string and its credentials out
of environment variables, documentation, diagnostics, and logs.

Restart the Forguncy application process after changing JWT rows in the
`config` table so the authentication composition is rebuilt.

Expose the login, refresh, and getinfo routes only through the Forguncy site's HTTPS
endpoint or an equivalent trusted network boundary. Never expose credentials
through an unprotected direct HTTP endpoint.

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
  "refresh_token": "<jwt>",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_expires_in": 604800
}
```

The authenticated user identity is carried in the JWT `sub` and `name` claims;
the login response does not repeat user details as a separate object.

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

## Refresh contract

### JSON request

```http
Content-Type: application/json
```

```json
{
  "refresh_token": "<jwt>"
}
```

### Form request

```http
Content-Type: application/x-www-form-urlencoded
```

```text
refresh_token=<jwt>
```

Multipart form data is not accepted.

### Responses

Successful refresh returns `200 OK`:

```json
{
  "access_token": "<jwt>",
  "refresh_token": "<jwt>",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_expires_in": 604800
}
```

The refresh response contains exactly the same five token fields and does not
include a user object.

Malformed input, missing fields, or an empty refresh token return `400 Bad Request`:

```json
{
  "error": "invalid_request"
}
```

An expired, malformed, unsigned, wrong-issuer, wrong-use, or otherwise invalid
refresh token returns `401 Unauthorized`:

```json
{
  "error": "invalid_refresh_token"
}
```

Unexpected server or configuration failures return `500 Internal Server Error`
with the same non-sensitive shape every time:

```json
{
  "error": "server_error"
}
```

Refresh JWTs are stateless. They are not persisted for per-token tracking and
cannot be revoked before expiry. This API does not promise refresh-token
rotation enforcement, logout-triggered invalidation, persistence, or immediate
disablement.

## Enterprise info contract

### Request

```http
GET /customapi/enterpriseapi/getinfo
Authorization: Bearer <access-token>
```

### Responses

Successful lookup returns `200 OK`:

```json
{
  "businessname": "<enterprise-name>",
  "creditcode": "<credit-code>",
  "county": "<county-name>"
}
```

Only these three enterprise fields are returned. The response does not include
`region`, internal identifiers, review fields, or update metadata.

Missing or invalid access tokens return `401 Unauthorized`:

```json
{
  "error": "invalid_access_token"
}
```

If the authenticated enterprise profile cannot be found, the API returns
`404 Not Found`:

```json
{
  "error": "enterprise_not_found"
}
```

Unexpected server or configuration failures return `500 Internal Server Error`
with the same fixed non-sensitive payload:

```json
{
  "error": "server_error"
}
```

## Test and release build

From `forguncy-server-api`, run:

```powershell
dotnet test .\tests\ForguncyServerApi.Tests\ForguncyServerApi.Tests.csproj --configuration Release --no-restore -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
dotnet build .\ForguncyServerApi.csproj --configuration Release --no-restore -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
```

The Release build produces:

```text
bin\Release\net472\ForguncyServerApi.dll
```

The build output contains the exact `SqlSugar` 5.1.4.111 and `MySql.Data`
8.0.30 dependencies and other compatible dependency DLLs used by this API.
The application, database, JWT, and diagnostics types are public so they can be
consumed directly by other .NET code. Because these public types are inspected
by the Forguncy 8.0.4 designer, upload the complete Release output directory
together with the API DLL; uploading only the API DLL is not sufficient for this
public surface.

The Release output now also contains the four JWT assemblies referenced from
the Forguncy 8.0.4 `Website\bin`. Keep the complete Release DLL bundle
together when uploading to the designer; uploading only the API DLL is not
sufficient for this public surface.

The application-specific DLLs in the final bundle are:

```text
ForguncyServerApi.dll
Google.Protobuf.dll
K4os.Compression.LZ4.dll
K4os.Compression.LZ4.Streams.dll
K4os.Hash.xxHash.dll
MySql.Data.dll
SqlSugar.dll
Ubiety.Dns.Core.dll
ZstdNet.dll
System.IdentityModel.Tokens.Jwt.dll
Microsoft.IdentityModel.JsonWebTokens.dll
Microsoft.IdentityModel.Tokens.dll
Microsoft.IdentityModel.Logging.dll
```

The remaining Microsoft/ASP.NET/Newtonsoft dependency DLLs in the output
directory are also required by the public reflection surface; upload the DLLs
from the same Release directory rather than mixing versions from another
build. Do not upload `.pdb` or `.config` files, and do not mix in EF Core,
Pomelo, or MySqlConnector DLLs for this net472 build.

## Upload to Forguncy

In the Forguncy designer, use:

```text
File -> Settings -> Custom Web API -> Upload Web API Assembly
```

Upload the DLL bundle from `bin\Release\net472`, including
`ForguncyServerApi.dll`, then configure JWT values through the Forguncy
`config` table as described above.
Configure the existing database connection only through the Forguncy `config`
table row where `item='ssl'`; the API does not initialize that database. The
designer integration intentionally contains only the `login`, `refresh`, and
`getinfo` routes; issue, validate, logout, and legacy auth aliases are
intentionally absent by design.
