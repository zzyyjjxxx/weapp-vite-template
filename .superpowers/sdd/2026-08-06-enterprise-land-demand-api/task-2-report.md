# Task 2 Report - Enterprise profile repository

Date: 2026-08-06

## Summary

Implemented the typed enterprise profile lookup requested by Task 2:

- added `EnterpriseProfile`
- added `IEnterpriseRepository`
- added `EnterpriseRepository` with the `m_preliminary_list` -> `yj_regioninfo` county join
- added `EnterpriseService`
- extended `SqlSugarPersistenceTests`
- added `EnterpriseServiceTests`

## TDD evidence

### RED

Command:

```powershell
dotnet test .\tests\ForguncyServerApi.Tests\ForguncyServerApi.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~SqlSugarPersistenceTests|FullyQualifiedName~EnterpriseServiceTests" -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
```

Exit code: `1`

Output:

```text
ForguncyServerApi -> C:\Users\18556\.codex\worktrees\e919\weapp-vite-template\forguncy-server-api\bin\Release\net472\ForguncyServerApi.dll
EnterpriseServiceTests.cs(49,53): error CS0246: 未能找到类型或命名空间名“IEnterpriseRepository”
EnterpriseServiceTests.cs(51,26): error CS0246: 未能找到类型或命名空间名“EnterpriseProfile”
EnterpriseServiceTests.cs(53,41): error CS0246: 未能找到类型或命名空间名“EnterpriseProfile”
EnterpriseServiceTests.cs(60,21): error CS0246: 未能找到类型或命名空间名“EnterpriseProfile”
```

Result: expected compile-time RED because the enterprise domain/repository/service types did not exist yet.

### GREEN

Command:

```powershell
dotnet test .\tests\ForguncyServerApi.Tests\ForguncyServerApi.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~SqlSugarPersistenceTests|FullyQualifiedName~EnterpriseServiceTests" -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
```

Exit code: `0`

Output:

```text
ForguncyServerApi -> C:\Users\18556\.codex\worktrees\e919\weapp-vite-template\forguncy-server-api\bin\Release\net472\ForguncyServerApi.dll
ForguncyServerApi.Tests -> C:\Users\18556\.codex\worktrees\e919\weapp-vite-template\forguncy-server-api\tests\ForguncyServerApi.Tests\bin\Release\net472\ForguncyServerApi.Tests.dll
总共 1 个测试文件与指定模式相匹配。

已通过! - 失败:     0，通过:     8，已跳过:     0，总计:     8，持续时间: 990 ms - ForguncyServerApi.Tests.dll (net472)
```

Result: focused GREEN passed for the new mapping + service coverage.

## Additional checks

Command:

```powershell
git diff --check -- forguncy-server-api
```

Exit code: `0`

Output:

```text
warning: in the working copy of 'forguncy-server-api/tests/ForguncyServerApi.Tests/Infrastructure/SqlSugarPersistenceTests.cs', LF will be replaced by CRLF the next time Git touches it
```

Interpretation: no diff-format errors; only a line-ending warning from Git.

## Changed files

- `forguncy-server-api/Domain/EnterpriseProfile.cs`
- `forguncy-server-api/Infrastructure/IEnterpriseRepository.cs`
- `forguncy-server-api/Infrastructure/EnterpriseRepository.cs`
- `forguncy-server-api/Application/EnterpriseService.cs`
- `forguncy-server-api/tests/ForguncyServerApi.Tests/Infrastructure/SqlSugarPersistenceTests.cs`
- `forguncy-server-api/tests/ForguncyServerApi.Tests/Application/EnterpriseServiceTests.cs`

## Self-review

- Followed the existing `UserRepository` client-factory pattern and kept the implementation on `SqlSugar` / `net472`.
- Kept the repository lookup keyed only by `creditCode`; `EnterpriseService` does not accept a caller-supplied override and uses `EnterpriseIdentity.CreditCode`.
- Mapped the expected `m_preliminary_list` columns (`businessName`, `creditCode`, `county`, `region`) plus the `yj_regioninfo` mapping (`id`, `name`).
- Kept public field exposure concerns out of this task; the repository/service return the internal profile model and no public API mapper was added here.
- Did not touch `skills-lock.json`, routes, or Task 1 production files.

## Concerns

- A broader `dotnet test` run for the whole `ForguncyServerApi.Tests` project still hits an existing API surface assertion in `AuthApiSurfaceTests.Api_assembly_exposes_the_public_application_types`. The failure output starts with:

```text
Expected: ["ForguncyServerApi.Api.AuthApi", ...]
Actual:   ["ForguncyServerApi.Api.AccessTokenReader", ...]
```

- That failure is outside the Task 2 brief and points at an existing Task 1 surface test. I left it unchanged per the task constraint not to modify Task 1 files.

## Fix round 1

Addressed the review findings by:

- mapping `m_preliminary_list.id` into `EnterpriseRow.Id`
- projecting that value into `EnterpriseLookupRow.UserId`
- assigning `EnterpriseProfile.UserId` from the repository result
- asserting the row `id` column mapping and the returned profile `UserId`
- removing the `BuildLookupSql` string/comment seam and deriving SQL-shape coverage from `BuildLookupQuery(...).ToSql()`

### Fix round 1 RED

Command:

```powershell
dotnet test .\tests\ForguncyServerApi.Tests\ForguncyServerApi.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~SqlSugarPersistenceTests|FullyQualifiedName~EnterpriseServiceTests" -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
```

Exit code: `1`

Output:

```text
ForguncyServerApi -> C:\Users\18556\.codex\worktrees\e919\weapp-vite-template\forguncy-server-api\bin\Release\net472\ForguncyServerApi.dll
ForguncyServerApi.Tests -> C:\Users\18556\.codex\worktrees\e919\weapp-vite-template\forguncy-server-api\tests\ForguncyServerApi.Tests\bin\Release\net472\ForguncyServerApi.Tests.dll
总共 1 个测试文件与指定模式相匹配。
失败 SqlSugarPersistenceTests.Enterprise_query_joins_regioninfo_by_county_and_filters_by_creditCode
Assert.Contains() Failure
Not found: enterprise`.`id` AS `UserId

失败 SqlSugarPersistenceTests.Enterprise_row_maps_to_the_real_m_preliminary_list_columns
Assert.NotNull() Failure
```

Interpretation: the amended tests correctly failed before the repository mapped or projected the enterprise `id`.

### Fix round 1 GREEN

Command:

```powershell
dotnet test .\tests\ForguncyServerApi.Tests\ForguncyServerApi.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~SqlSugarPersistenceTests|FullyQualifiedName~EnterpriseServiceTests" -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
```

Exit code: `0`

Output:

```text
ForguncyServerApi -> C:\Users\18556\.codex\worktrees\e919\weapp-vite-template\forguncy-server-api\bin\Release\net472\ForguncyServerApi.dll
ForguncyServerApi.Tests -> C:\Users\18556\.codex\worktrees\e919\weapp-vite-template\forguncy-server-api\tests\ForguncyServerApi.Tests\bin\Release\net472\ForguncyServerApi.Tests.dll
总共 1 个测试文件与指定模式相匹配。

已通过! - 失败:     0，通过:     8，已跳过:     0，总计:     8，持续时间: 1 s - ForguncyServerApi.Tests.dll (net472)
```

Interpretation: the focused Task 2 mapping/service coverage passed after the explicit `UserId` mapping and projection fix.
