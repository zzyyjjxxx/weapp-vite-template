# Forguncy enterprise land-demand Web API

This class library targets Forguncy 8.0.4 and exposes eight custom Web API routes:

```text
POST /customapi/enterpriseapi/login
POST /customapi/enterpriseapi/refresh
GET /customapi/enterpriseapi/getinfo
POST /customapi/enterpriseapi/sendcode
POST /customapi/enterpriseapi/verifycode
GET /customapi/landdemandapi/getlanddemand
POST /customapi/landdemandapi/addlanddemand
POST /customapi/landdemandapi/updatelanddemand
```

The five Forguncy/IdentityModel compile-time assemblies required by the
Forguncy 8.0.4 target are included in `lib/` and both project files reference
that repository directory by default. `ForguncyBin` can still be overridden
when building against a deliberately selected external Forguncy SDK.

The enterprise routes own login, refresh, and enterprise profile lookup. The
land-demand routes own querying, creating, and updating an enterprise's filing
record. There are no issue, validate, logout, or legacy alias routes.

## Authentication and ownership

Login identifies the enterprise by the `c_userinfo` record whose `creditCode`
equals the submitted `username`. The existing `c_userinfo.password` value must
use the lowercase middle 16 characters of the password's MD5 digest:

```text
lowercaseHex(MD5(UTF8(password))).Substring(8, 16)
```

A matching enterprise can log in only when `c_userinfo.isopen` equals the
integer `1`.

The login response carries an access JWT and a refresh JWT. The access JWT
holds the user id in the `sub` claim and the enterprise credit code in the
JWT `name` claim. Business ownership is derived exclusively from the validated
access token's `name` claim; it is never read from the request body, query
string, or path. Only the refresh route accepts a refresh token, and it
consumes nothing else.

## SMS verification contract

`sendcode` and `verifycode` require the same bearer access token as the other
business routes. The enterprise credit code is read only from the validated
JWT `name` claim. Requests accept either JSON or URL-encoded form data:

```json
POST /customapi/enterpriseapi/sendcode
{"mobile":"13800000000"}

POST /customapi/enterpriseapi/verifycode
{"mobile":"13800000000","code":"123456"}
```

The send response is `200` with `{"success":true,"status":"success"}` and
the `expires_at`/`retry_at` timestamps. A repeated request during the
60-second cooldown returns `429` with status `cooldown`; an external send
failure returns `502` with status `failed`. Verification returns `200` for
success, `400` with status `failed` for a wrong code, and `410` with status
`expired` when the one-time code is missing, used, or expired.

Before sending, the API reads `client_id`, `client_secret`, and `tenant` from
the Forguncy `config` table, calls the configured SMS authentication endpoint,
and immediately stores its `data` value in `config.item='token'`. It then
reads `zzqscode` and `zzqssecret`, writes `m_message_log`, calls the SMS
endpoint with `zzqscode`, `zzqssecret`, and `Auth-Token`, and updates the log
by `transactionID`: successful calls store `retMsg`, while failed calls store
the response `message`. The runtime clients are replaceable; local tests use
mock authentication and SMS clients and never call the internal network.

Missing, malformed, expired, wrong-key, wrong-use, or otherwise invalid access
tokens return `401 Unauthorized` with `{"error":"invalid_token"}` for every
business operation. The land-demand write routes validate the access token
before reading the request body, so an unauthenticated request cannot use body
parsing as an oracle.

## Enterprise info contract

The getinfo route resolves the enterprise profile from `m_preliminary_list`
and joins the county name through `m_preliminary_list.county` to
`yj_regioninfo.id`. It returns exactly these four fields and nothing else:

```json
{
  "businessname": "<enterprise-name>",
  "creditcode": "<credit-code>",
  "county": "<county-name>",
  "region": "<township-or-region>"
}
```

If the authenticated enterprise profile cannot be found, the route returns
`404 Not Found` with `{"error":"enterprise_not_found"}`. The enterprise API
never updates internal columns.

## Land-demand contracts

The three land-demand routes operate on the enterprise's filing record in
`landusedemand_info`, scoped by the access token identity. The write JSON
accepts exactly the 26 approved writable fields; identity, audit, and internal
property names (for example `id`, `updatetime`, `updateuser`,
`region_remark`, `county_isrecommend`, `reviewstatus`, and
`review_opinion`) are rejected with `400 invalid_request` before any
persistence work.

`landusedemand` is required and must be `1` (submit) or `2` (draft). All
remaining fields are optional draft values; the existing validation layer
enforces the conditional rules, decimal precision, and units (万元 for
`investment`, `pred_ys`, `pred_tax`, `pred_rdex`; 万元/吨标煤 for
`pred_unitenergy`) when a record is submitted.

The query and both write routes return the saved filing with exactly the 31
fields below, including `updatetime` formatted as `yyyy-MM-dd HH:mm:ss`.

### Land-demand response JSON

```json
{
  "area": "330212",
  "building_area": 1234.56,
  "businessname": "Synthetic Enterprise",
  "contact": "Synthetic Contact",
  "county": "Synthetic County",
  "creditcode": "91330200SYNTHETIC",
  "deploy_height": 12.34,
  "deploy_landtype": "Industrial",
  "deploy_park": "Park A,Park B",
  "deploy_weight": 56.78,
  "expect_park": "Synthetic Park",
  "expect_time": "2026-12",
  "financing_money": 100,
  "financing_time": "2026-12",
  "futureindustry": "Synthetic Direction",
  "investment": 12345678901234.123456,
  "is_deploy": "1",
  "is_financing": "0",
  "is_specialuse": "1",
  "keyindustry": "Synthetic Track",
  "landusedemand": "1",
  "office": "Synthetic Office",
  "phone": "13800000000",
  "pred_rdex": 300.000003,
  "pred_tax": 200.000002,
  "pred_unitenergy": 400.000004,
  "pred_ys": 100.000001,
  "project_hydm": "C3990",
  "projectdata": "Synthetic project",
  "region": "330212000000",
  "updatetime": "2026-08-06 12:34:56"
}
```

### Land-demand write JSON

```json
{
  "area": "330212",
  "building_area": 1234.56,
  "contact": "Synthetic Contact",
  "deploy_height": 12.34,
  "deploy_landtype": "Industrial",
  "deploy_park": "Park A,Park B",
  "deploy_weight": 56.78,
  "expect_park": "Synthetic Park",
  "expect_time": "2026-12",
  "financing_money": 100,
  "financing_time": "2026-12",
  "futureindustry": "Synthetic Direction",
  "investment": 12345678901234.123456,
  "is_deploy": "1",
  "is_financing": "0",
  "is_specialuse": "1",
  "keyindustry": "Synthetic Track",
  "landusedemand": "1",
  "office": "Synthetic Office",
  "phone": "13800000000",
  "pred_rdex": 300.000003,
  "pred_tax": 200.000002,
  "pred_unitenergy": 400.000004,
  "pred_ys": 100.000001,
  "project_hydm": "C3990",
  "projectdata": "Synthetic project"
}
```

The add route returns `409 Conflict` with
`{"error":"land_demand_exists"}` when a filing already exists for the
enterprise. The update route returns `404 Not Found` with
`{"error":"land_demand_not_found"}` when no filing exists.

## Request formats

Login and refresh accept `application/json` or
`application/x-www-form-urlencoded`. Multipart form data is not accepted.
Login accepts `username` and `password`; refresh accepts `refresh_token`.

The two SMS verification routes accept `application/json` or
`application/x-www-form-urlencoded`. The three land-demand routes require
`application/json` (with an optional `charset` parameter) and reject any
other media type.

## Error contract

Every error response is the fixed non-sensitive JSON shape shown below, and
responses never expose exception details, configuration values, database
connection information, signing keys, SQL text, or credentials.

| Route | Status | Body |
| --- | --- | --- |
| Login | 400 | `{"error":"invalid_request"}` |
| Login | 401 | `{"error":"invalid_credentials"}` |
| Refresh | 400 | `{"error":"invalid_request"}` |
| Refresh | 401 | `{"error":"invalid_refresh_token"}` |
| Business operations | 401 | `{"error":"invalid_token"}` |
| Business operations | 404 | `{"error":"enterprise_not_found"}` |
| Send code | 429 | `{"success":false,"status":"cooldown"}` |
| Send code | 502 | `{"success":false,"status":"failed"}` |
| Verify code | 400 | `{"success":false,"status":"failed"}` |
| Verify code | 410 | `{"success":false,"status":"expired"}` |
| Get land demand | 404 | `{"error":"land_demand_not_found"}` |
| Add land demand | 409 | `{"error":"land_demand_exists"}` |
| All routes | 500 | `{"error":"server_error"}` |

Request cancellation is always propagated and never converted into a `500`
response.

## Database contract

Forguncy supplies the existing database and tables (`c_userinfo`,
`m_preliminary_list`, `landusedemand_info`, `yj_regioninfo`, and
`m_message_log`). The API runtime does not create or alter schemas, seed
enterprises, or initialize database content. Apply
`sql/002-create-enterprise-sms-verification.sql` once before deploying the
SMS routes; it creates the one-row-per-enterprise verification table.

The MySQL connection is not configured through environment variables. The
existing database connection is selected by `config.item='ssl'`: set its
connection string in that row's `value` column. The Forguncy 8.0.4 SDK
`IDataAccess` reads that row, and the application uses its `value` as the
SqlSugar/MySQL connection string. The lookup intentionally does not filter on
`enable`. Keep the connection string and its credentials out of environment
variables, documentation, diagnostics, and logs.

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
| `client_id` | SMS authentication client id | Required; no default. |
| `client_secret` | SMS authentication client secret | Required; no default. |
| `tenant` | SMS authentication tenant | Required; no default. |
| `token` | Latest SMS authentication token | Written after successful authentication. |
| `zzqscode` | SMS service code | Required; no default. |
| `zzqssecret` | SMS service secret | Required; no default. |

Existing non-blank values are used as-is. The signing key must contain at
least 32 characters and the expiration values must be positive integers.
Invalid non-blank values fail initialization instead of being overwritten. Do
not put JWT values in environment variables or commit them to source control.

Restart the Forguncy application process after changing JWT rows in the
`config` table so the composition is rebuilt.

Expose all eight routes only through the Forguncy site's HTTPS endpoint or an
equivalent trusted network boundary. Never expose credentials through an
unprotected direct HTTP endpoint.

## HTML API quick test

api-test.html is a dependency-free browser test page for all eight routes.
Set the API base URL, log in, and then use the access token to call the
enterprise, SMS, and land-demand operations. The add/update payloads are
editable JSON and start with a draft example (landusedemand=2).

Serve the file through a local static server instead of opening it directly
as file:// when the API does not enable CORS:

```powershell
python -m http.server 8765
```

Then open http://localhost:8765/api-test.html?base=http://localhost:8080.
The page stores only test tokens in sessionStorage; it does not store the
login password.

## Test and release build

From `forguncy-server-api`, run:

```powershell
dotnet test .\tests\ForguncyServerApi.Tests\ForguncyServerApi.Tests.csproj --configuration Release --no-restore
dotnet build .\ForguncyServerApi.csproj --configuration Release --no-restore
```

The Release build produces:

```text
bin\Release\net472\ForguncyServerApi.dll
```

The build output contains the exact `SqlSugar` 5.1.4.111 and `MySql.Data`
8.0.30 dependencies and other compatible dependency DLLs used by this API.
The application, database, JWT, and diagnostics types are public so they can
be consumed directly by other .NET code. Because these public types are
inspected by the Forguncy 8.0.4 designer, upload the complete Release output
directory together with the API DLL; uploading only the API DLL is not
sufficient for this public surface.

The Release output also contains the four JWT assemblies referenced from the
Forguncy 8.0.4 `Website\bin`. Keep the complete Release DLL bundle together
when uploading to the designer.

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
`config` table as described above. Configure the existing database connection
only through the Forguncy `config` table row where `item='ssl'`; the API does
not initialize that database. The designer integration intentionally contains
only the six documented routes; issue, validate, logout, and legacy aliases
are intentionally absent by design.
