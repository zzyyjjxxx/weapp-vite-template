# Forguncy SDK config connection string design

## Goal

Replace the environment-variable database connection string with the connection
string stored by Forguncy in the `config` table row whose `item` column equals
`ssl`. The row's `value` column is used unchanged as the EF Core/MySQL
connection string.

## Confirmed scope

- The only public route remains `POST /customapi/authapi/login`.
- JWT issuance and validation remain internal services.
- The lookup uses the installed Forguncy 8.0.4 SDK's
  `GrapeCity.Forguncy.ServerApi.IDataAccess.GetTableData` API.
- The exact SDK query is `GetTableData("config", new ColumnValuePair
  { ColumnName = "item", Value = "ssl" })`.
- The `enable` column is not part of the lookup condition; this follows the
  explicit requirement to select by `item='ssl'` only.
- The returned `value` must be a non-empty string. A missing row, missing value,
  null value, or blank value is a configuration failure and is returned to the
  client only as the existing fixed `500 {"error":"server_error"}` response.
- The connection string and any SDK/database exception details are never logged
  or returned.

## Architecture and data flow

`AuthApi` passes its inherited Forguncy `DataAccess` property into
`AuthCompositionRoot.CreateAsync`. The composition root constructs a small
SDK-backed config reader, reads the `ssl` row before creating EF Core options,
and passes the resolved string to `AuthDbContextOptionsFactory`. EF Core
continues to own only the `jwt_users` schema and authentication data access;
the Forguncy SDK owns the `config` lookup.

`AuthOptions` continues to parse JWT and optional bootstrap settings, but no
longer requires or reads `FGC_AUTH_MYSQL_CONNECTION`.

## Error handling

The existing `AuthApi` outer exception boundary covers request parsing,
configuration, SDK lookup, EF initialization, and login. Any unexpected error
is recorded through the existing sanitized diagnostic path and produces the
fixed non-sensitive `500` response. Cancellation continues to propagate.

## Verification

- Unit tests prove `item='ssl'` is the exact SDK query and that valid, missing,
  null, blank, and non-string `value` cases are handled correctly.
- Configuration tests prove the removed environment variable is not required.
- Composition/API tests prove the SDK data-access dependency is passed into the
  login composition root without adding any public route.
- Release tests and the .NET 6 production build use the Forguncy 8.0.4 `bin`
  path. Live MySQL/login and active designer smoke checks remain environment
  dependent and must not be claimed unless actually observed.
