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
