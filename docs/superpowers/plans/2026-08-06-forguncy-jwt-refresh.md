# Forguncy JWT Refresh Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend the existing Forguncy 8.0.4 login API with a stateless JWT refresh token, a `Refresh` POST route, and the corresponding config, tests, and deployment documentation.

**Architecture:** Keep `AuthApi` as the Forguncy HTTP adapter, `AuthService` as the application layer, and `JwtTokenService` as the only JWT implementation. Login queries `c_userinfo` as today; refresh validates the signed refresh JWT and reconstructs the internal user from `sub` and `name` without a database read. Both operations return an internal token-pair result that the HTTP layer serializes without a user object.

**Tech Stack:** Forguncy 8.0.4 Server API, .NET Framework 4.7.2, C# 10, `System.IdentityModel.Tokens.Jwt` and matching Microsoft IdentityModel assemblies from the Forguncy 8.0.4 SDK, SqlSugar 5.1.4.111, MySql.Data 8.0.30, Newtonsoft.Json, xUnit.

## Global Constraints

- Build only for `net472` against `D:\Program Files\Forguncy 8.0.4\Website\bin`.
- Keep `SqlSugar` 5.1.4.111, `MySql.Data` 8.0.30, and all existing Forguncy-compatible dependency versions unchanged.
- Read all JWT settings from Forguncy `config` rows; use `FGC_JWT_REFRESH_EXPIRES_MINUTES` for the refresh lifetime and persist the default `10080` minutes when the row is missing or blank.
- Do not add a database table, refresh-token persistence, a logout endpoint, or external issue/validate endpoints.
- Use the existing `c_userinfo` contract and lowercase middle-16 MD5 password behavior without schema changes.
- Never serialize a `user` object in login or refresh responses; identity remains in JWT `sub` and `name` claims.
- Keep invalid refresh tokens indistinguishable as `401 {"error":"invalid_refresh_token"}` and keep unexpected failures as fixed non-sensitive `500` responses.
- Do not stage or remove the existing untracked `forguncy-server-api/.vs/` directory.
- Every task ends with a focused test cycle and its own commit.

---

### Task 1: Add refresh lifetime configuration and token-pair contracts

**Files:**
- Modify: `forguncy-server-api/Configuration/AuthOptions.cs`
- Modify: `forguncy-server-api/Infrastructure/ForguncyJwtConfigurationReader.cs`
- Modify: `forguncy-server-api/Application/LoginModels.cs`
- Test: `forguncy-server-api/tests/ForguncyServerApi.Tests/Configuration/AuthOptionsTests.cs`
- Test: `forguncy-server-api/tests/ForguncyServerApi.Tests/Infrastructure/ForguncyJwtConfigurationReaderTests.cs`

**Interfaces:**
- `AuthOptions` produces `JwtSigningKey`, `JwtIssuer`, `JwtLifetime`, and `JwtRefreshLifetime`.
- `ForguncyJwtConfigurationReader.ReadOrCreate(IDataAccess)` returns the existing three keys plus `FGC_JWT_REFRESH_EXPIRES_MINUTES`.
- `TokenPair` contains `AccessToken`, `RefreshToken`, `ExpiresInSeconds`, and `RefreshExpiresInSeconds`.
- `LoginResult` becomes `(LoginStatus Status, TokenPair? Tokens, AuthUser? User)`.
- `RefreshResult` becomes `(RefreshStatus Status, TokenPair? Tokens)` with `Success`, `InvalidRequest`, and `InvalidToken` statuses.

- [ ] **Step 1: Write failing configuration tests.**

Add tests that assert the fourth constructor parameter is `TimeSpan`, valid values parse `FGC_JWT_REFRESH_EXPIRES_MINUTES=120`, the missing-value default is `TimeSpan.FromMinutes(10080)`, and the configuration reader adds/updates/reads the fourth item alongside the existing three items. The missing-row assertions must expect four additions and the exact persisted value `"10080"`.

```csharp
[Fact]
public void From_uses_a_seven_day_default_refresh_lifetime()
{
    var options = AuthOptions.From(ValidValues());

    Assert.Equal(TimeSpan.FromMinutes(10080), options.JwtRefreshLifetime);
}

[Fact]
public void From_parses_refresh_expiration_in_minutes()
{
    var values = ValidValues();
    values["FGC_JWT_REFRESH_EXPIRES_MINUTES"] = "120";

    Assert.Equal(TimeSpan.FromMinutes(120), AuthOptions.From(values).JwtRefreshLifetime);
}
```

- [ ] **Step 2: Run the focused tests and verify the expected RED failure.**

Run from `D:\WorkProject\weapp-vite-template\.worktrees\forguncy-jwt-login`:

```powershell
dotnet test .\forguncy-server-api\tests\ForguncyServerApi.Tests\ForguncyServerApi.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~AuthOptionsTests|FullyQualifiedName~ForguncyJwtConfigurationReaderTests" -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
```

Expected result: compilation or assertions fail because the fourth JWT setting and token-pair types do not yet exist; no production implementation is written before this RED result.

- [ ] **Step 3: Implement the minimum configuration and model changes.**

Extend the positional `AuthOptions` record with `TimeSpan JwtRefreshLifetime`. Parse `FGC_JWT_REFRESH_EXPIRES_MINUTES` using the same positive-integer and overflow checks as `FGC_JWT_EXPIRES_MINUTES`, with `10080` minutes as the absent-value default. Add the reader constant and default factory to the existing read-then-insert/update flow. Add these records/enums to `LoginModels.cs`:

```csharp
public sealed record TokenPair(
    string AccessToken,
    string RefreshToken,
    int ExpiresInSeconds,
    int RefreshExpiresInSeconds);

public enum RefreshStatus
{
    Success,
    InvalidRequest,
    InvalidToken
}

public sealed record LoginResult(LoginStatus Status, TokenPair? Tokens, AuthUser? User);

public sealed record RefreshResult(RefreshStatus Status, TokenPair? Tokens);
```

- [ ] **Step 4: Run the focused tests and verify GREEN.**

Run the same focused `dotnet test` command. Expected result: all configuration and reader tests pass, including four-row read/insert/update behavior and the seven-day default.

- [ ] **Step 5: Commit the task.**

```powershell
git add -- forguncy-server-api/Configuration/AuthOptions.cs forguncy-server-api/Infrastructure/ForguncyJwtConfigurationReader.cs forguncy-server-api/Application/LoginModels.cs forguncy-server-api/tests/ForguncyServerApi.Tests/Configuration/AuthOptionsTests.cs forguncy-server-api/tests/ForguncyServerApi.Tests/Infrastructure/ForguncyJwtConfigurationReaderTests.cs
git commit -m "feat: add JWT refresh configuration"
```

### Task 2: Generate and validate refresh JWTs in the application layer

**Files:**
- Modify: `forguncy-server-api/Security/IJwtTokenService.cs`
- Modify: `forguncy-server-api/Security/JwtTokenService.cs`
- Modify: `forguncy-server-api/Application/AuthService.cs`
- Modify: `forguncy-server-api/Api/AuthCompositionRoot.cs`
- Test: `forguncy-server-api/tests/ForguncyServerApi.Tests/Security/JwtTokenServiceTests.cs`
- Test: `forguncy-server-api/tests/ForguncyServerApi.Tests/Application/AuthServiceTests.cs`

**Interfaces:**
- `IJwtTokenService.CreateToken(AuthUser)` remains the access-token method.
- `IJwtTokenService.CreateRefreshToken(AuthUser)` creates a refresh JWT.
- `IJwtTokenService.ValidateToken(string)` keeps general signed-token validation behavior.
- `IJwtTokenService.ValidateRefreshToken(string)` validates a signed token and requires `token_use=refresh`.
- `AuthService` constructor accepts both access and refresh lifetimes.
- `AuthService.RefreshAsync(string refreshToken, CancellationToken)` returns `RefreshResult` and never queries `IUserRepository`.

- [ ] **Step 1: Write failing JWT and application tests.**

Add tests that create a refresh JWT with `token_use=refresh`, preserve `sub/name`, use the refresh lifetime, and reject an access JWT through `ValidateRefreshToken`. Add application tests that successful login returns a complete `TokenPair`, a valid refresh creates another pair from claims, an empty token returns `InvalidRequest`, malformed/expired/wrong-type tokens return `InvalidToken`, and the user repository call count remains zero during refresh.

```csharp
[Fact]
public void CreateRefreshToken_contains_refresh_use_and_validate_accepts_it()
{
    var service = new JwtTokenService(TestOptions());
    var token = service.CreateRefreshToken(new AuthUser { Id = 7, Username = "demo" });

    var principal = service.ValidateRefreshToken(token);

    Assert.Equal("7", principal.FindFirst("sub")?.Value);
    Assert.Equal("demo", principal.FindFirst("name")?.Value);
    Assert.Equal("refresh", principal.FindFirst("token_use")?.Value);
}

[Fact]
public void ValidateRefreshToken_rejects_an_access_token()
{
    var service = new JwtTokenService(TestOptions());
    var token = service.CreateToken(new AuthUser { Id = 7, Username = "demo" });

    Assert.Throws<SecurityTokenException>(() => service.ValidateRefreshToken(token));
}
```

- [ ] **Step 2: Run the focused tests and verify the expected RED failure.**

```powershell
dotnet test .\forguncy-server-api\tests\ForguncyServerApi.Tests\ForguncyServerApi.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~JwtTokenServiceTests|FullyQualifiedName~AuthServiceTests" -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
```

Expected result: compilation fails because the interface methods, token pair result, and refresh application method are absent.

- [ ] **Step 3: Implement the minimum JWT and application behavior.**

Refactor `JwtTokenService` around one private creation method that accepts lifetime and `token_use`. `CreateToken` uses `options.JwtLifetime` and `access`; `CreateRefreshToken` uses `options.JwtRefreshLifetime` and `refresh`. Keep HS256, issuer, `sub`, `name`, `jti`, `iat`, `nbf`, `exp`, zero clock skew, and no audience validation. `ValidateRefreshToken` calls the existing signed-token validation path and throws `SecurityTokenException` unless `token_use` equals `refresh`.

Update `AuthService.LoginAsync` to create a `TokenPair` and preserve the existing username trim, dummy-hash verification, `isopen == 1`, and invalid-credential timing behavior. Add `RefreshAsync` that rejects blank input, calls `ValidateRefreshToken`, parses a positive integer `sub` and nonblank `name`, creates an internal `AuthUser`, and returns a fresh pair without accessing `IUserRepository`. Catch only token/claim validation failures and map them to `RefreshStatus.InvalidToken`; cancellation must still propagate.

Update all test `StubTokens` implementations and `AuthCompositionRoot` to pass both lifetimes from `AuthOptions`.

- [ ] **Step 4: Run the focused tests and verify GREEN.**

Run the same focused test command. Expected result: JWT and application tests pass, including access-token rejection, claim parsing, expiry selection, invalid-token mapping, and no database lookup during refresh.

- [ ] **Step 5: Commit the task.**

```powershell
git add -- forguncy-server-api/Security/IJwtTokenService.cs forguncy-server-api/Security/JwtTokenService.cs forguncy-server-api/Application/AuthService.cs forguncy-server-api/Api/AuthCompositionRoot.cs forguncy-server-api/tests/ForguncyServerApi.Tests/Security/JwtTokenServiceTests.cs forguncy-server-api/tests/ForguncyServerApi.Tests/Application/AuthServiceTests.cs
git commit -m "feat: issue and validate JWT refresh tokens"
```

### Task 3: Add the Forguncy refresh route and request/response mapping

**Files:**
- Modify: `forguncy-server-api/Api/LoginRequestReader.cs`
- Modify: `forguncy-server-api/Api/AuthApi.cs`
- Modify: `forguncy-server-api/Api/AuthDiagnostics.cs`
- Test: `forguncy-server-api/tests/ForguncyServerApi.Tests/Api/LoginRequestReaderTests.cs`
- Test: `forguncy-server-api/tests/ForguncyServerApi.Tests/Api/AuthApiSurfaceTests.cs`

**Interfaces:**
- `LoginRequestReader.ReadRefreshTokenAsync(HttpRequest, CancellationToken)` reads a JSON or URL-encoded `refresh_token` field and throws the existing `LoginRequestFormatException` for unsupported/malformed/missing payloads.
- `AuthApi` exposes exactly two declared public methods, parameterless `Task Login()` and parameterless `Task Refresh()`, each with `[Post]`.
- Login and refresh success payloads use one five-field `TokenResponse` shape and never include `user`.
- `AuthDiagnostics` keeps the existing login diagnostic contract and adds a separate refresh operation code/event without logging exception objects or sensitive messages.

- [ ] **Step 1: Write failing request-reader and API-surface tests.**

Add JSON and form tests for `ReadRefreshTokenAsync`, plus missing-field and malformed-JSON tests. Update the reflection test from “only the login post method” to exact `Login` and `Refresh` methods, both `[Post]` and neither `[Get]`. Update response-mapping tests to construct `LoginResult`/`RefreshResult` with a `TokenPair` and expect:

```json
{"access_token":"signed-access","refresh_token":"signed-refresh","token_type":"Bearer","expires_in":3600,"refresh_expires_in":604800}
```

Both `expires_in` and `refresh_expires_in` are expressed in seconds. The
configuration item `FGC_JWT_REFRESH_EXPIRES_MINUTES` remains in minutes, so
the default `10080` minutes is represented as `604800` seconds in responses.

Add assertions that serialized success JSON contains no `user` property, that invalid refresh results map to `400 invalid_request` and `401 invalid_refresh_token`, and that refresh exceptions use a fixed `500 server_error` response.

- [ ] **Step 2: Run the focused API tests and verify the expected RED failure.**

```powershell
dotnet test .\forguncy-server-api\tests\ForguncyServerApi.Tests\ForguncyServerApi.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~LoginRequestReaderTests|FullyQualifiedName~AuthApiSurfaceTests" -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
```

Expected result: compilation or assertions fail because the refresh reader, route, response fields, and mapping do not yet exist.

- [ ] **Step 3: Implement the minimum HTTP behavior.**

Extend `LoginRequestReader` using the existing content-type detection and stream/form handling. Add `Refresh` to `AuthApi` with the same cancellation, fixed error, composition-cache, and sanitized-exception structure as `Login`, but call `AuthService.RefreshAsync`. Share a private token-response mapper so both `CreateLoginResponse` and `CreateRefreshResponse` serialize the same five fields. Preserve the login operation code and add `auth.refresh.unexpected_failure` for refresh failures through `AuthDiagnostics`.

- [ ] **Step 4: Run the focused API tests and verify GREEN.**

Run the same focused command. Expected result: request parsing, route reflection, response JSON, error status, no-user-payload, and diagnostics tests pass.

- [ ] **Step 5: Commit the task.**

```powershell
git add -- forguncy-server-api/Api/LoginRequestReader.cs forguncy-server-api/Api/AuthApi.cs forguncy-server-api/Api/AuthDiagnostics.cs forguncy-server-api/tests/ForguncyServerApi.Tests/Api/LoginRequestReaderTests.cs forguncy-server-api/tests/ForguncyServerApi.Tests/Api/AuthApiSurfaceTests.cs
git commit -m "feat: add JWT refresh API"
```

### Task 4: Update deployment documentation and complete release verification

**Files:**
- Modify: `forguncy-server-api/README.md`
- Modify: `reports/verification.md`
- Test: `forguncy-server-api/tests/ForguncyServerApi.Tests/Api/AuthApiSurfaceTests.cs` (final exported-type and reflection expectations)

**Interfaces:**
- README documents `POST /customapi/authapi/refresh`, the five-field response, `FGC_JWT_REFRESH_EXPIRES_MINUTES=10080`, and the stateless/no-revocation limitation.
- `reports/verification.md` records only commands actually run in this implementation cycle; it does not claim a Forguncy designer interaction that was not observed.

- [ ] **Step 1: Write the documentation assertions before editing README.**

Extend the existing deployment-surface test to require the refresh route, `refresh_token`, `refresh_expires_in`, and `FGC_JWT_REFRESH_EXPIRES_MINUTES`, while asserting that the README does not promise database persistence or revocation. The public exported-type expectation must include `TokenPair`, `RefreshResult`, and `RefreshStatus` if they remain public records/enums.

- [ ] **Step 2: Run the documentation/surface test and verify the expected RED failure.**

```powershell
dotnet test .\forguncy-server-api\tests\ForguncyServerApi.Tests\ForguncyServerApi.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~AuthApiSurfaceTests" -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
```

Expected result: the new README and exported-surface assertions fail until the documentation and final public surface are updated.

- [ ] **Step 3: Update README and verification records.**

Document both routes, request formats, exact success/error JSON, the new config item/default, the lack of user-object response, and the fact that old stateless refresh JWTs cannot be revoked before expiry. Add the actual focused-test results, full-test result, Release build result, API reflection result, and `git diff --check` result to `reports/verification.md` without recording credentials or connection strings.

- [ ] **Step 4: Run the complete release verification.**

Run:

```powershell
dotnet test .\forguncy-server-api\tests\ForguncyServerApi.Tests\ForguncyServerApi.Tests.csproj --configuration Release --no-restore -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
dotnet build .\forguncy-server-api\ForguncyServerApi.csproj --configuration Release --no-restore -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
git diff --check
```

Then load `bin\Release\net472\ForguncyServerApi.dll` with the Forguncy 8.0.4 SDK assembly and confirm the exported `ForguncyServerApi.Api.AuthApi` type derives from `GrapeCity.Forguncy.ServerApi.ForguncyApi`, with parameterless `[Post]` methods `Login` and `Refresh` returning `Task`.

- [ ] **Step 5: Commit the task.**

```powershell
git add -- forguncy-server-api/README.md reports/verification.md forguncy-server-api/tests/ForguncyServerApi.Tests/Api/AuthApiSurfaceTests.cs
git commit -m "docs: document JWT refresh flow"
```
