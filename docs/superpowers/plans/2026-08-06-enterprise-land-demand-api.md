# Enterprise Authentication and Land Demand Web API Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the ambiguous Forguncy auth routes with explicit enterprise login/refresh/info APIs and add bearer-token-isolated land-demand query, create, and update APIs backed by the existing MySQL tables.

**Architecture:** `EnterpriseApi` owns enterprise login, refresh, and profile lookup; `LandDemandApi` owns the three filing operations. Both use one cached `EnterpriseCompositionRoot` containing the existing `AuthService`, `JwtTokenService`, typed repositories, and application services. The access-token adapter requires `token_use=access` and derives the credit code from the JWT `name` claim; all SQL remains parameterized through SqlSugar and the existing Forguncy `config.item='ssl'` connection source.

**Tech Stack:** Forguncy 8.0.4 Server API, .NET Framework 4.7.2, C# 10, SqlSugar 5.1.4.111, MySql.Data 8.0.30, Newtonsoft.Json, Microsoft IdentityModel JWT assemblies, ASP.NET Core HTTP abstractions, and xUnit.

## Global Constraints

- Build only for `net472` against `D:\Program Files\Forguncy 8.0.4\Website\bin`.
- Keep `SqlSugar` 5.1.4.111, `MySql.Data` 8.0.30, and all existing Forguncy-compatible dependency versions unchanged.
- Use `c_userinfo` for enterprise credential verification, `m_preliminary_list` joined to `yj_regioninfo` for enterprise info, and `landusedemand_info` for filing records.
- Expose only `/customapi/enterpriseapi/login`, `/customapi/enterpriseapi/refresh`, `/customapi/enterpriseapi/getinfo`, `/customapi/landdemandapi/getlanddemand`, `/customapi/landdemandapi/addlanddemand`, and `/customapi/landdemandapi/updatelanddemand`; remove the old `authapi` login/refresh methods and do not add compatibility aliases.
- Derive every business-operation credit code from the access JWT `name` claim. Reject missing, malformed, expired, wrong-issuer, wrong-signing-key, or refresh-use tokens with fixed `401 {"error":"invalid_token"}` responses.
- Never accept or update `creditcode`, `businessname`, `county`, `region`, `id`, `updatetime`, `updateuser`, review opinions, recommendation states, or other internal columns from an enterprise write request.
- Return only the approved filing fields plus `landusedemand` and `updatetime`; do not serialize internal review/audit fields.
- `landusedemand=1` is submitted and `landusedemand=2` is draft. Drafts may be incomplete but submitted records must satisfy the existing conditional required-field rules.
- Read the MySQL connection from Forguncy `config.item='ssl'`; do not add hardcoded credentials, connection strings, database initialization, schema migration, or live test writes.
- Each task ends with a focused test run and its own commit. Preserve the user-owned modified `skills-lock.json` and unrelated files.

---

### Task 1: Enforce access-token identity and shared JSON response behavior

**Files:**
- Create: `forguncy-server-api/Api/AccessTokenReader.cs`
- Create: `forguncy-server-api/Api/ApiResponseWriter.cs`
- Create: `forguncy-server-api/Application/EnterpriseIdentity.cs`
- Modify: `forguncy-server-api/Security/IJwtTokenService.cs`
- Modify: `forguncy-server-api/Security/JwtTokenService.cs`
- Test: `forguncy-server-api/tests/ForguncyServerApi.Tests/Security/JwtTokenServiceTests.cs`
- Test: `forguncy-server-api/tests/ForguncyServerApi.Tests/Api/AccessTokenReaderTests.cs`

**Interfaces:**
- Add `ClaimsPrincipal ValidateAccessToken(string token)` to `IJwtTokenService`.
- Add `AccessTokenReader.ReadRequiredIdentity(HttpRequest request, IJwtTokenService tokens, CancellationToken cancellationToken)` returning `EnterpriseIdentity(int UserId, string CreditCode)` and throwing a private-format exception for missing or malformed bearer headers and invalid access tokens.
- Add `ApiResponseWriter.WriteJsonAsync(HttpResponse response, int statusCode, object value, CancellationToken cancellationToken)`; it writes UTF-8 JSON, `application/json; charset=utf-8`, `Cache-Control: no-store`, and `Pragma: no-cache`.

- [ ] **Step 1: Write the failing JWT and header tests.**

Add the following behaviors before production changes:

```csharp
[Fact]
public void ValidateAccessToken_accepts_an_access_token_and_preserves_identity_claims()
{
    var service = TestJwtTokenService();
    var token = service.CreateToken(new AuthUser { Id = 7, Username = "91330200SYNTHETIC" });

    var principal = service.ValidateAccessToken(token);

    Assert.Equal("7", principal.FindFirst("sub")?.Value);
    Assert.Equal("91330200SYNTHETIC", principal.FindFirst("name")?.Value);
    Assert.Equal("access", principal.FindFirst("token_use")?.Value);
}

[Fact]
public void ValidateAccessToken_rejects_a_refresh_token()
{
    var service = TestJwtTokenService();
    var token = service.CreateRefreshToken(new AuthUser { Id = 7, Username = "91330200SYNTHETIC" });

    Assert.Throws<SecurityTokenException>(() => service.ValidateAccessToken(token));
}
```

Add `DefaultHttpContext` tests for an exact `Bearer <access-token>` header, a missing header, a non-Bearer scheme, a blank token, and a valid refresh token. The invalid cases must all throw the same `AccessTokenFormatException` without exposing the JWT or exception detail.

- [ ] **Step 2: Run the focused tests and verify RED.**

Run from `forguncy-server-api`:

```powershell
dotnet test .\tests\ForguncyServerApi.Tests\ForguncyServerApi.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~JwtTokenServiceTests|FullyQualifiedName~AccessTokenReaderTests" -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
```

Expected result: compilation fails because `ValidateAccessToken`, `EnterpriseIdentity`, `AccessTokenReader`, and `ApiResponseWriter` do not exist.

- [ ] **Step 3: Implement the minimum shared primitives.**

Refactor the existing private signed-token path so `ValidateAccessToken` calls it and then requires `token_use` to equal `access` with ordinal comparison. Keep `ValidateRefreshToken` requiring `refresh`; do not weaken the existing issuer, lifetime, algorithm, or zero-clock-skew checks. Make `AccessTokenReader` parse only the `Authorization` header, validate with `ValidateAccessToken`, require a positive integer `sub` and nonblank `name`, and return the credit code in `EnterpriseIdentity`. Make `ApiResponseWriter` contain the response-header and Newtonsoft JSON serialization code currently duplicated in `AuthApi`.

- [ ] **Step 4: Run the focused tests and verify GREEN.**

Run the same filtered `dotnet test` command. Expected result: all access-token and header tests pass, including the writer test's assertions for content type, no-store cache headers, pragma headers, and UTF-8 JSON.

- [ ] **Step 5: Commit the task.**

```powershell
git add -- forguncy-server-api/Api/AccessTokenReader.cs forguncy-server-api/Api/ApiResponseWriter.cs forguncy-server-api/Application/EnterpriseIdentity.cs forguncy-server-api/Security/IJwtTokenService.cs forguncy-server-api/Security/JwtTokenService.cs forguncy-server-api/tests/ForguncyServerApi.Tests/Security/JwtTokenServiceTests.cs forguncy-server-api/tests/ForguncyServerApi.Tests/Api/AccessTokenReaderTests.cs
git commit -m "feat: enforce enterprise access token identity"
```

### Task 2: Add typed enterprise profile lookup and county association

**Files:**
- Create: `forguncy-server-api/Domain/EnterpriseProfile.cs`
- Create: `forguncy-server-api/Infrastructure/IEnterpriseRepository.cs`
- Create: `forguncy-server-api/Infrastructure/EnterpriseRepository.cs`
- Create: `forguncy-server-api/Application/EnterpriseService.cs`
- Modify: `forguncy-server-api/tests/ForguncyServerApi.Tests/Infrastructure/SqlSugarPersistenceTests.cs`
- Create: `forguncy-server-api/tests/ForguncyServerApi.Tests/Application/EnterpriseServiceTests.cs`

**Interfaces:**
- `EnterpriseProfile` stores the internal `UserId`, `CreditCode`, `BusinessName`, `CountyName`, and `Region`; the API response mapper exposes only the approved four public fields: `businessname`, `creditcode`, `county`, and `region`.
- `IEnterpriseRepository.FindByCreditCodeAsync(string creditCode, CancellationToken cancellationToken)` returns `EnterpriseProfile?`.
- `EnterpriseService.GetProfileAsync(EnterpriseIdentity identity, CancellationToken cancellationToken)` returns the profile or `null` and never accepts a caller-supplied credit code.

- [ ] **Step 1: Write failing mapping and service tests.**

Extend `SqlSugarPersistenceTests` with reflection assertions that the enterprise row type maps `businessName`, `creditCode`, `county`, and `region` to `m_preliminary_list`, and that the region row type maps `id` and `name` to `yj_regioninfo`. Add a SQL-shape assertion from `EnterpriseRepository` that contains both table names, the `creditCode` predicate, and the `yj_regioninfo.id` join. Add service tests that a matching profile is returned with county name and region, a missing profile returns `null`, and the repository receives only `identity.CreditCode`.

```csharp
[Fact]
public async Task GetProfileAsync_uses_the_authenticated_credit_code_and_returns_joined_county_name()
{
    var repository = new StubEnterpriseRepository(new EnterpriseProfile
    {
        UserId = 7,
        CreditCode = "91330200SYNTHETIC",
        BusinessName = "Synthetic Enterprise",
        CountyName = "鄞州区",
        Region = "首南街道"
    });
    var service = new EnterpriseService(repository);

    var result = await service.GetProfileAsync(
        new EnterpriseIdentity(7, "91330200SYNTHETIC"),
        CancellationToken.None);

    Assert.Equal("91330200SYNTHETIC", repository.LastCreditCode);
    Assert.Equal("鄞州区", result!.CountyName);
    Assert.Equal("首南街道", result.Region);
}
```

- [ ] **Step 2: Run the focused tests and verify RED.**

```powershell
dotnet test .\tests\ForguncyServerApi.Tests\ForguncyServerApi.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~SqlSugarPersistenceTests|FullyQualifiedName~EnterpriseServiceTests" -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
```

Expected result: compilation fails because the enterprise domain, repository, and service types do not exist.

- [ ] **Step 3: Implement the typed join.**

Create private persistence rows with `SugarTable("m_preliminary_list")` and `SugarTable("yj_regioninfo")`. Map `county` to a string ID and use a SqlSugar inner join `enterprise.CountyId == region.Id`, filtered by `enterprise.CreditCode == creditCode`, selecting the four needed source values. Create `EnterpriseRepository` with the same client-factory dependency pattern as `UserRepository`, cancellation checks before and after the query, and `SingleAsync()` semantics. `EnterpriseService` validates a nonblank identity credit code, calls the repository, and returns the profile unchanged.

- [ ] **Step 4: Run the focused tests and verify GREEN.**

Run the same filtered command. Expected result: mapping, SQL-shape, county association, missing-profile, and ownership tests pass.

- [ ] **Step 5: Commit the task.**

```powershell
git add -- forguncy-server-api/Domain/EnterpriseProfile.cs forguncy-server-api/Infrastructure/IEnterpriseRepository.cs forguncy-server-api/Infrastructure/EnterpriseRepository.cs forguncy-server-api/Application/EnterpriseService.cs forguncy-server-api/tests/ForguncyServerApi.Tests/Infrastructure/SqlSugarPersistenceTests.cs forguncy-server-api/tests/ForguncyServerApi.Tests/Application/EnterpriseServiceTests.cs
git commit -m "feat: add enterprise profile repository"
```

### Task 3: Add typed land-demand persistence, response whitelist, and write validation

**Files:**
- Create: `forguncy-server-api/Domain/LandDemandRecord.cs`
- Create: `forguncy-server-api/Application/LandDemandModels.cs`
- Create: `forguncy-server-api/Application/LandDemandValidation.cs`
- Create: `forguncy-server-api/Application/LandDemandService.cs`
- Create: `forguncy-server-api/Infrastructure/ILandDemandRepository.cs`
- Create: `forguncy-server-api/Infrastructure/LandDemandRepository.cs`
- Create: `forguncy-server-api/tests/ForguncyServerApi.Tests/Application/LandDemandServiceTests.cs`
- Create: `forguncy-server-api/tests/ForguncyServerApi.Tests/Application/LandDemandValidationTests.cs`
- Modify: `forguncy-server-api/tests/ForguncyServerApi.Tests/Infrastructure/SqlSugarPersistenceTests.cs`

**Interfaces:**
- `LandDemandWriteRequest` contains the writable filing fields: `area`, `building_area`, `expect_park`, `expect_time`, `is_deploy`, `deploy_park`, `is_specialuse`, `deploy_landtype`, `deploy_height`, `deploy_weight`, `investment`, `project_hydm`, `keyindustry`, `futureindustry`, `pred_ys`, `pred_tax`, `pred_rdex`, `pred_unitenergy`, `projectdata`, `is_financing`, `financing_money`, `financing_time`, `contact`, `office`, `phone`, and `landusedemand`.
- `LandDemandResponse` contains exactly these 31 properties, using nullable decimals for decimal columns and strings for varchar/status/time columns: `businessname`, `creditcode`, `county`, `region`, `area`, `building_area`, `expect_park`, `expect_time`, `is_deploy`, `deploy_park`, `is_specialuse`, `deploy_landtype`, `deploy_height`, `deploy_weight`, `investment`, `project_hydm`, `keyindustry`, `futureindustry`, `pred_ys`, `pred_tax`, `pred_rdex`, `pred_unitenergy`, `projectdata`, `is_financing`, `financing_money`, `financing_time`, `contact`, `office`, `phone`, `landusedemand`, and `updatetime`.
- `ILandDemandRepository` exposes:

```csharp
Task<LandDemandRecord?> FindByCreditCodeAsync(string creditCode, CancellationToken cancellationToken);
Task<LandDemandRecord> InsertAsync(LandDemandRecord record, CancellationToken cancellationToken);
Task<bool> UpdateWritableFieldsAsync(
    string creditCode,
    LandDemandWriteRequest request,
    string updateTime,
    string updateUser,
    CancellationToken cancellationToken);
```

- `LandDemandService.GetAsync(EnterpriseIdentity, CancellationToken)` returns a `LandDemandOperationResult` with `Success`, `EnterpriseNotFound`, or `NotFound`.
- `LandDemandService.AddAsync(EnterpriseIdentity, LandDemandWriteRequest, CancellationToken)` returns `Success`, `EnterpriseNotFound`, `Exists`, or `InvalidRequest`.
- `LandDemandService.UpdateAsync(EnterpriseIdentity, LandDemandWriteRequest, CancellationToken)` returns `Success`, `EnterpriseNotFound`, `NotFound`, or `InvalidRequest`.
- Successful service results carry a `LandDemandResponse`; timestamps use one injected `Func<DateTimeOffset>` so tests can assert exact `updatetime` values.

Use one explicit result contract so the API and tests share the same status names:

```csharp
public enum LandDemandOperationStatus
{
    Success,
    EnterpriseNotFound,
    NotFound,
    Exists,
    InvalidRequest
}

public sealed record LandDemandOperationResult(
    LandDemandOperationStatus Status,
    LandDemandResponse? Record);
```

- [ ] **Step 1: Write failing persistence, validation, and service tests.**

Add mapping assertions for every response/write column in `landusedemand_info`, including `id`, `county`, `region`, `businessname`, `creditcode`, `landusedemand`, all approved filing columns, `updatetime`, and `updateuser`. Add validation tests for:

```csharp
[Theory]
[InlineData("1")]
[InlineData("2")]
public void Validate_accepts_only_draft_or_submitted_status(string status)
{
    Assert.True(LandDemandValidation.IsSupportedStatus(status));
}

[Fact]
public void Validate_submission_requires_financing_fields_when_financing_is_one()
{
    var request = ValidSubmittedRequest() with
    {
        IsFinancing = "1",
        FinancingMoney = null,
        FinancingTime = null
    };

    var errors = LandDemandValidation.Validate(request);

    Assert.Contains("financing_money", errors.Select(error => error.Field));
    Assert.Contains("financing_time", errors.Select(error => error.Field));
}
```

Add service tests that query only by authenticated credit code; an absent enterprise profile returns `EnterpriseNotFound`; a duplicate add returns `Exists` without calling insert; a missing update returns `NotFound`; a successful add fills business name/county/region from the enterprise profile and sets `updatetime`/`updateuser`; a successful update leaves the protected identity values and fake internal repository columns unchanged; and an input containing no identity fields cannot alter those values. Use an in-memory stub repository at the service seam, not a live database.

- [ ] **Step 2: Run the focused tests and verify RED.**

```powershell
dotnet test .\tests\ForguncyServerApi.Tests\ForguncyServerApi.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~SqlSugarPersistenceTests|FullyQualifiedName~LandDemandValidationTests|FullyQualifiedName~LandDemandServiceTests" -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
```

Expected result: compilation fails because the land-demand types and repository contract do not exist.

- [ ] **Step 3: Implement the typed row, whitelist, and service.**

Map only existing `landusedemand_info` columns; do not add the nonexistent `newproject` column from the frontend mock contract. Use `decimal?` for `investment`, `building_area`, `deploy_height`, `deploy_weight`, `financing_money`, `pred_tax`, `pred_rdex`, `pred_ys`, and `pred_unitenergy`; use strings for varchar columns. Build `LandDemandResponse` explicitly from the stored row so `id`, review fields, `updateuser`, `industryCode`, energy backfill fields, and registration metadata cannot leak.

Implement `LandDemandValidation` with these exact rules:

- status must be `1` or `2`;
- draft validates supplied values but permits missing fields;
- submitted records require `area`, `building_area`, `expect_park`, `expect_time`, `is_deploy`, `is_specialuse`, `investment`, `project_hydm`, `keyindustry`, `futureindustry`, `pred_ys`, `pred_tax`, `pred_rdex`, `pred_unitenergy`, `projectdata`, `is_financing`, `contact`, and `phone`;
- `deploy_park` is required when `is_deploy` is the affirmative value, `deploy_landtype` is required when `is_specialuse` is affirmative, and `financing_money`/`financing_time` are required when `is_financing` is `1`;
- decimal values must be nonnegative and fit their existing database precision; `expect_time` and `financing_time` remain strings with the existing `YYYY-MM` validation; `projectdata` receives no application character-limit check.

Use `InsertAsync` with an explicit row populated from the authenticated `EnterpriseProfile`, and use `Updateable<LandDemandRecord>().SetColumns(...)` or an equivalent explicit column list so omitted internal columns are never included in the SQL update. Set `updatetime` from the injected clock and `updateuser` to the authenticated credit code. Convert duplicate-key failures to the service `Exists` status without returning database details.

- [ ] **Step 4: Run the focused tests and verify GREEN.**

Run the same filtered command. Expected result: all mapping, validation, ownership, duplicate, missing-record, protected-field, timestamp, and status tests pass.

- [ ] **Step 5: Commit the task.**

```powershell
git add -- forguncy-server-api/Domain/LandDemandRecord.cs forguncy-server-api/Application/LandDemandModels.cs forguncy-server-api/Application/LandDemandValidation.cs forguncy-server-api/Application/LandDemandService.cs forguncy-server-api/Infrastructure/ILandDemandRepository.cs forguncy-server-api/Infrastructure/LandDemandRepository.cs forguncy-server-api/tests/ForguncyServerApi.Tests/Application/LandDemandServiceTests.cs forguncy-server-api/tests/ForguncyServerApi.Tests/Application/LandDemandValidationTests.cs forguncy-server-api/tests/ForguncyServerApi.Tests/Infrastructure/SqlSugarPersistenceTests.cs
git commit -m "feat: add land demand persistence service"
```

### Task 4: Replace AuthApi with EnterpriseApi and wire the shared composition root

**Files:**
- Create: `forguncy-server-api/Api/EnterpriseCompositionRoot.cs`
- Create: `forguncy-server-api/Api/EnterpriseApi.cs`
- Create: `forguncy-server-api/Api/EnterpriseDiagnostics.cs`
- Delete: `forguncy-server-api/Api/AuthApi.cs`
- Delete: `forguncy-server-api/Api/AuthCompositionRoot.cs`
- Delete: `forguncy-server-api/Api/AuthDiagnostics.cs`
- Modify: `forguncy-server-api/Application/AuthService.cs`
- Test: `forguncy-server-api/tests/ForguncyServerApi.Tests/Api/AuthApiSurfaceTests.cs`
- Test: `forguncy-server-api/tests/ForguncyServerApi.Tests/Api/LoginRequestReaderTests.cs`

**Interfaces:**
- `EnterpriseCompositionRoot.CreateAsync(IDataAccess dataAccess, CancellationToken cancellationToken)` returns a cached root containing `AuthService`, `EnterpriseService`, `LandDemandService`, `IJwtTokenService`, and the shared SqlSugar client factory.
- `EnterpriseApi` derives from `ForguncyApi` and declares exactly three public parameterless `Task` methods: `[Post] Login()`, `[Post] Refresh()`, and `[Get] GetInfo()`.
- `EnterpriseDiagnostics` records only sanitized operation code and exception type for `enterprise.login`, `enterprise.refresh`, and `enterprise.get_info` failures.

- [ ] **Step 1: Write failing surface and handler tests.**

Change `AuthApiSurfaceTests` to load `ForguncyServerApi.Api.EnterpriseApi`, assert it derives from `ForguncyApi`, and require exactly `Login`, `Refresh`, and `GetInfo` with the specified attributes and no `AuthApi` exported type. Update exported-type assertions for `EnterpriseCompositionRoot`, `EnterpriseService`, and the new domain/application types. Add response tests for the existing five-field token JSON and for `GetInfo` returning only `businessname`, `creditcode`, `county`, and `region`; assert that `id`, `updateuser`, and review fields are absent. Add tests for `GetInfo` mapping to 401, 404, and fixed 500 responses. Preserve the existing login/refresh JSON and form-reader tests under the new enterprise surface.

- [ ] **Step 2: Run the focused surface tests and verify RED.**

```powershell
dotnet test .\tests\ForguncyServerApi.Tests\ForguncyServerApi.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~AuthApiSurfaceTests|FullyQualifiedName~LoginRequestReaderTests" -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
```

Expected result: compilation or reflection assertions fail because the old `AuthApi` still exists and the enterprise API/root/diagnostics surface is absent.

- [ ] **Step 3: Implement the enterprise composition and HTTP adapter.**

Move the current auth composition into `EnterpriseCompositionRoot` and add the typed enterprise and land-demand services from Tasks 2 and 3. Use one static `RetryableAsyncCache<EnterpriseCompositionRoot>` shared by both API classes. `Login` and `Refresh` retain the existing request readers and `AuthService` result mapping, but write through `ApiResponseWriter` and record enterprise operation diagnostics. `GetInfo` reads `EnterpriseIdentity` from `AccessTokenReader`, calls `EnterpriseService`, maps a missing profile to `404 enterprise_not_found`, serializes a four-property response record including `region`, and catches unexpected errors as fixed `500 server_error`. Remove the old files/classes so no ambiguous auth route remains.

- [ ] **Step 4: Run the focused surface tests and verify GREEN.**

Run the same filtered command. Expected result: enterprise route reflection, login/refresh request contracts, token serialization, enterprise info whitelist, auth failures, and sanitized 500 behavior pass.

- [ ] **Step 5: Commit the task.**

```powershell
git add -- forguncy-server-api/Api/EnterpriseCompositionRoot.cs forguncy-server-api/Api/EnterpriseApi.cs forguncy-server-api/Api/EnterpriseDiagnostics.cs forguncy-server-api/Api/AuthApi.cs forguncy-server-api/Api/AuthCompositionRoot.cs forguncy-server-api/Api/AuthDiagnostics.cs forguncy-server-api/Application/AuthService.cs forguncy-server-api/tests/ForguncyServerApi.Tests/Api/AuthApiSurfaceTests.cs forguncy-server-api/tests/ForguncyServerApi.Tests/Api/LoginRequestReaderTests.cs
git commit -m "feat: expose explicit enterprise auth api"
```

### Task 5: Add LandDemandApi request parsing and HTTP status mapping

**Files:**
- Create: `forguncy-server-api/Api/LandDemandRequestReader.cs`
- Create: `forguncy-server-api/Api/LandDemandApi.cs`
- Create: `forguncy-server-api/Api/LandDemandDiagnostics.cs`
- Create: `forguncy-server-api/tests/ForguncyServerApi.Tests/Api/LandDemandRequestReaderTests.cs`
- Create: `forguncy-server-api/tests/ForguncyServerApi.Tests/Api/LandDemandApiSurfaceTests.cs`

**Interfaces:**
- `LandDemandRequestReader.ReadAsync(HttpRequest request, CancellationToken cancellationToken)` accepts only `application/json`, parses `LandDemandWriteRequest`, rejects malformed JSON, missing/incorrect property types, unsupported media types, and any identity/audit/internal property names.
- `LandDemandApi` derives from `ForguncyApi` and declares exactly three public parameterless `Task` methods: `[Get] GetLandDemand()`, `[Post] AddLandDemand()`, and `[Post] UpdateLandDemand()`.
- `LandDemandApi` maps service results to the fixed errors `invalid_token`, `invalid_request`, `enterprise_not_found`, `land_demand_not_found`, `land_demand_exists`, and `server_error`.

- [ ] **Step 1: Write failing request-reader and API-surface tests.**

Add `DefaultHttpContext` tests for a valid JSON write request covering every writable property, a missing optional draft property, a decimal/null property, malformed JSON, a non-string status, an identity-field injection such as `creditcode`, an audit-field injection such as `updateuser`, and `application/x-www-form-urlencoded`. Add reflection tests for the three methods, attributes, parameterless `Task` signatures, and no public helper methods. Add handler tests for:

```csharp
[Fact]
public async Task GetLandDemand_returns_only_the_filing_whitelist_and_update_time()
{
    var response = await InvokeGetLandDemandAsync();

    Assert.Equal(200, response.StatusCode);
    var json = JObject.Parse(response.Body);
    Assert.NotNull(json["projectdata"]);
    Assert.NotNull(json["updatetime"]);
    Assert.Null(json["id"]);
    Assert.Null(json["updateuser"]);
    Assert.Null(json["region_remark"]);
    Assert.Null(json["county_isrecommend"]);
}
```

Also cover 404 for a missing record, 409 for duplicate add, 400 for invalid input, 401 for a refresh token, and fixed 500 for repository exceptions. Use a fake composition root/service seam; do not connect the HTTP unit tests to MySQL.

- [ ] **Step 2: Run the focused API tests and verify RED.**

```powershell
dotnet test .\tests\ForguncyServerApi.Tests\ForguncyServerApi.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~LandDemandRequestReaderTests|FullyQualifiedName~LandDemandApiSurfaceTests" -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
```

Expected result: compilation fails because the request reader and route class do not exist.

- [ ] **Step 3: Implement the JSON adapter and route mapping.**

Use `JObject.Parse` and an explicit case-sensitive allowlist for the writable property names from `LandDemandWriteRequest`. Convert JSON numeric tokens to nullable decimals and keep varchar/status/date values as strings; reject identity, audit, and internal property names before deserialization. In `LandDemandApi`, read and validate the bearer identity before reading a write body, so an unauthorized request cannot use body parsing as an oracle. Call `LandDemandService.GetAsync`, `AddAsync`, or `UpdateAsync`, map each result enum to the documented status/body, serialize the explicit `LandDemandResponse`, and record only sanitized operation diagnostics. All three handlers must propagate request cancellation and never turn it into a 500 response.

- [ ] **Step 4: Run the focused API tests and verify GREEN.**

Run the same filtered command. Expected result: JSON parsing, identity-field rejection, route surface, whitelist serialization, status mapping, cancellation, and sanitized error tests pass.

- [ ] **Step 5: Commit the task.**

```powershell
git add -- forguncy-server-api/Api/LandDemandRequestReader.cs forguncy-server-api/Api/LandDemandApi.cs forguncy-server-api/Api/LandDemandDiagnostics.cs forguncy-server-api/tests/ForguncyServerApi.Tests/Api/LandDemandRequestReaderTests.cs forguncy-server-api/tests/ForguncyServerApi.Tests/Api/LandDemandApiSurfaceTests.cs
git commit -m "feat: add land demand web api"
```

### Task 6: Update deployment documentation and complete release verification

**Files:**
- Modify: `forguncy-server-api/README.md`
- Modify: `reports/verification.md`
- Modify: `forguncy-server-api/tests/ForguncyServerApi.Tests/Api/AuthApiSurfaceTests.cs`
- Modify: `forguncy-server-api/tests/ForguncyServerApi.Tests/Api/LandDemandApiSurfaceTests.cs`

**Interfaces:**
- README documents the six formal routes, access-token ownership, refresh-token-only refresh behavior, the enterprise response whitelist, the land-demand response/write whitelist, exact error bodies, and the existing `config.item='ssl'` connection source.
- Verification report records only commands and evidence observed during this implementation cycle. It must state that local MySQL schema/connection checks are read-only and that no live Forguncy HTTP/Designer interaction or real land-demand write is claimed unless actually observed.

- [ ] **Step 1: Write failing documentation-contract assertions.**

Add source assertions that `forguncy-server-api/README.md` contains the six formal paths, `enterpriseapi`, `landdemandapi`, `getinfo`, `getlanddemand`, `addlanddemand`, `updatelanddemand`, `invalid_token`, `land_demand_exists`, `updatetime`, and `config.item='ssl'`; assert it no longer documents `/customapi/authapi/login` or `/customapi/authapi/refresh` and does not contain password-bearing connection-string patterns. Add assertions that the API response tests enumerate every approved response property and exclude the internal properties.

- [ ] **Step 2: Run the documentation tests and verify RED.**

```powershell
dotnet test .\tests\ForguncyServerApi.Tests\ForguncyServerApi.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~AuthApiSurfaceTests|FullyQualifiedName~LandDemandApiSurfaceTests" -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
```

Expected result: the new documentation assertions fail until README and final surface expectations are updated.

- [ ] **Step 3: Update README and append actual verification evidence.**

Replace the old auth-only route list and examples with the enterprise and land-demand contracts from the design spec. Document that enterprise login still verifies `c_userinfo`, that `getinfo` joins `m_preliminary_list.county` to `yj_regioninfo.id`, that business ownership comes from the access token, and that the enterprise API cannot update internal columns. Do not copy any local connection string or credential into README or `reports/verification.md`. Append command, exit code, test count, build warnings/errors, reflection result, `git diff --check`, and read-only MySQL preflight results actually observed in this run.

- [ ] **Step 4: Run complete static/release verification.**

Run from `forguncy-server-api`:

```powershell
dotnet test .\tests\ForguncyServerApi.Tests\ForguncyServerApi.Tests.csproj --configuration Release --no-restore -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
dotnet build .\ForguncyServerApi.csproj --configuration Release --no-restore -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
git diff --check
```

Load `bin\Release\net472\ForguncyServerApi.dll` with the Forguncy 8.0.4 SDK assembly and assert that `EnterpriseApi` and `LandDemandApi` derive from `GrapeCity.Forguncy.ServerApi.ForguncyApi`, expose only the documented parameterless methods and attributes, and contain no `AuthApi` type. With the task-scoped local database secret supplied through the process environment and never printed, run this read-only MySQL query for table existence, column comments, the enterprise/region join coverage, and the filing unique key; do not run `INSERT`, `UPDATE`, `DELETE`, or schema DDL:

```sql
SELECT TABLE_NAME, TABLE_TYPE
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'mujunbigdata'
  AND TABLE_NAME IN ('m_preliminary_list', 'landusedemand_info', 'yj_regioninfo');
SELECT TABLE_NAME, COLUMN_NAME, COLUMN_TYPE, IS_NULLABLE, COLUMN_KEY, COLUMN_COMMENT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'mujunbigdata'
  AND TABLE_NAME IN ('m_preliminary_list', 'landusedemand_info', 'yj_regioninfo')
ORDER BY TABLE_NAME, ORDINAL_POSITION;
SELECT COUNT(*) AS enterprise_rows, COUNT(region.id) AS county_matches
FROM m_preliminary_list enterprise
LEFT JOIN yj_regioninfo region ON region.id = enterprise.county;
SELECT TABLE_NAME, CONSTRAINT_NAME, COLUMN_NAME
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = 'mujunbigdata'
  AND TABLE_NAME = 'landusedemand_info';
```

- [ ] **Step 5: Run final verification and commit the documentation.**

Re-run the full test command after README/report changes, confirm the final `git status --short` contains only the intended feature commits plus the pre-existing `skills-lock.json` modification, and commit only the documentation/report/surface-test files:

```powershell
git add -- forguncy-server-api/README.md reports/verification.md forguncy-server-api/tests/ForguncyServerApi.Tests/Api/AuthApiSurfaceTests.cs forguncy-server-api/tests/ForguncyServerApi.Tests/Api/LandDemandApiSurfaceTests.cs
git commit -m "docs: document enterprise land demand api"
```

The final handoff must distinguish unit/build/reflection success from unavailable live Forguncy Designer or HTTP verification and must not claim a real database write was tested.
