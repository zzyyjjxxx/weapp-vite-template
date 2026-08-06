# Task 4 report - Enterprise auth API surface

Date: 2026-08-06

Scope completed:

- Replaced `AuthApi` / `AuthCompositionRoot` / `AuthDiagnostics` with `EnterpriseApi` / `EnterpriseCompositionRoot` / `EnterpriseDiagnostics`.
- Preserved the existing login and refresh request readers plus the five-field token response contract.
- Added `GET /customapi/enterpriseapi/getinfo` with a three-field response: `businessname`, `creditcode`, `county`.
- Wired `AuthService`, `EnterpriseService`, `LandDemandService`, `IJwtTokenService`, and the shared SqlSugar client factory into one cached root.
- Removed the old auth route aliases from the production surface and updated the README route documentation.

TDD evidence:

1. RED

   Command:

   ```powershell
   dotnet test .\tests\ForguncyServerApi.Tests\ForguncyServerApi.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~AuthApiSurfaceTests|FullyQualifiedName~LoginRequestReaderTests" -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
   ```

   Result:

   - Exit code: 1
   - Expected failure observed because `EnterpriseApi` / `EnterpriseCompositionRoot` / `EnterpriseDiagnostics` were not present yet and the old `AuthApi` route surface still existed.

2. GREEN

   Same focused command rerun after implementation.

   Result:

   - Exit code: 0
   - Passed: 43
   - Failed: 0
   - Skipped: 0

Fresh verification evidence:

1. Full test project

   ```powershell
   dotnet test .\tests\ForguncyServerApi.Tests\ForguncyServerApi.Tests.csproj --configuration Release --no-restore -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
   ```

   Result:

   - Exit code: 0
   - Passed: 168
   - Failed: 0
   - Skipped: 0

2. Release build

   ```powershell
   dotnet build .\ForguncyServerApi.csproj --configuration Release --no-restore -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
   ```

   Result:

   - Exit code: 0
   - Warnings: 0
   - Errors: 0

3. Diff hygiene

   ```powershell
   git diff --check
   ```

   Result:

   - Exit code: 0
   - Reported only CRLF normalization warnings on touched files and the unrelated pre-existing dirty `skills-lock.json`.

Notes / limitations:

- `LandDemandApi` was not implemented or modified in this task.
- No credentials were added and no live database writes were performed during verification.

## Fix round 1 - review follow-up

Scope completed:

- Moved cached composition-root ownership into `EnterpriseCompositionRoot.GetOrCreateAsync(...)` so future APIs can reuse the same cached root instead of `EnterpriseApi` owning a private cache.
- Added a nonpublic test-only composition-root factory override seam in `EnterpriseApi` and exercised the public `GetInfo()` handler end-to-end without MySQL.
- Updated the access-token business contract from `invalid_access_token` to `invalid_token` and switched `EnterpriseApi` to a typed `AccessTokenFormatException` catch.
- Updated README wording for the `getinfo` invalid-token contract.

TDD evidence:

1. RED

   Command:

   ```powershell
   dotnet test .\tests\ForguncyServerApi.Tests\ForguncyServerApi.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~AuthApiSurfaceTests|FullyQualifiedName~LoginRequestReaderTests" -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
   ```

   Result:

   - Exit code: 1
   - Expected failures observed for the missing shared `EnterpriseCompositionRoot.GetOrCreateAsync(...)` facility, missing nonpublic handler-test seam, and stale `{"error":"invalid_access_token"}` response contract.

2. GREEN

   Same focused command rerun after implementation.

   Result:

   - Exit code: 0
   - Passed: 51
   - Failed: 0
   - Skipped: 0

Fresh verification evidence:

1. Full test project

   ```powershell
   dotnet test .\tests\ForguncyServerApi.Tests\ForguncyServerApi.Tests.csproj --configuration Release --no-restore -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
   ```

   Result:

   - Exit code: 0
   - Passed: 176
   - Failed: 0
   - Skipped: 0

2. Release build

   ```powershell
   dotnet build .\ForguncyServerApi.csproj --configuration Release --no-restore -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
   ```

   Result:

   - Exit code: 0
   - Warnings: 0
   - Errors: 0

3. Diff hygiene

   ```powershell
   git diff --check
   ```

   Result:

   - Exit code: 0
   - Reported only CRLF normalization warnings on touched files and the unrelated pre-existing dirty `skills-lock.json`.

4. Forguncy SDK reflection surface

   Loaded the Release assembly together with the Forguncy 8.0.4 Server API assembly and inspected `EnterpriseApi`.

   Result:

   - Base type: `GrapeCity.Forguncy.ServerApi.ForguncyApi`
   - Public constructors: one parameterless constructor
   - Public parameterless `Task` handlers: exactly `Login`, `Refresh`, and `GetInfo`
   - Route attributes: `[Post] Login`, `[Post] Refresh`, and `[Get] GetInfo`

Scope confirmation:

- `LandDemandApi` has no diff.
- The unrelated pre-existing `skills-lock.json` change is excluded from this task and commit.
