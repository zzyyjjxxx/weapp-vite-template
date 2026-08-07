# Task 6 Report: Forguncy database bootstrap and deployment documentation

## Scope

Implemented Task 6 on branch `codex/forguncy-jwt-login` in the isolated
`forguncy-jwt-login` worktree. This was the documentation/configuration
exception; no behavior TDD changes were required.

## Files created

- `forguncy-server-api/sql/001-create-database.sql`
  - Contains only the requested `CREATE DATABASE IF NOT EXISTS forguncy_auth`
    statement with `utf8mb4` character set and collation.
  - Does not define `jwt_users`; EF Core `EnsureCreatedAsync` remains the sole
    owner of that table schema.
- `forguncy-server-api/README.md`
  - Documents the single `POST /customapi/authapi/login` route.
  - Documents JSON and `application/x-www-form-urlencoded` requests,
    `200`/`400`/`401`/safe `500` responses, configuration variables, local SQL
    setup, test and Release build commands, the generated DLL, Forguncy
    designer upload path, and the intentional absence of issue/validate routes.
  - Uses placeholders and environment-variable instructions only; no real
    password, connection string, signing key, or other secret was added.

## Checks and actual results

1. `git diff --check`
   - Exit code `0`; clean.
2. Credential scan from the brief (sensitive search argument redacted in this
   report):

   ```powershell
   rg -n --hidden -g '!node_modules' -g '!forguncy-server-api/bin' -g '!forguncy-server-api/obj' '<redacted-user-provided-password>' forguncy-server-api
   ```

   - The historical command used the user-provided value in place of the
     placeholder above. It produced no matching output; `rg` exit code `1` was
     the expected no-match result. No real secrets were found.
3. SQL exact-content check
   - Pass; the file matches the sole SQL statement required by the brief.
4. README required-content check
   - Pass; all required route, request, response, environment variable,
     command, DLL, upload-path, and absent-route text was present.
5. Pre-commit worktree check
   - Only the two requested files were staged.

`dotnet test` and the Release build were not rerun because this task is
explicitly a documentation/configuration exception and the brief required the
documentation checks rather than behavior changes. The README records the
commands operators should run.

## Commit

- Subject: `docs: document Forguncy login deployment`
- Commit: `174476585880fcf09b7e065453c316e1e2309473`

## Self-review

- SQL is minimal and credential-free.
- README examples never populate a secret or include a user-provided MySQL
  password.
- Configuration names and defaults match `AuthOptions`.
- Response payloads match `AuthApi`, including the fixed non-sensitive 500
  payload.
- The route and designer upload path match the completed API contract.
- No source, project, report, or unrelated worktree file was included in the
  commit.

## Concerns

- The Release build and live MySQL/API invocation were not run in this
  documentation-only task; live verification would require deployment-only
  environment values and is outside the brief.
- Git reported normal Windows line-ending warnings for the two newly created
  LF files; this did not affect `git diff --check`.

## Task 6 fix

- Updated only the bootstrap environment variable names in `forguncy-server-api/README.md`:
  `FGC_AUTH_BOOTSTRAP_USERNAME` and `FGC_AUTH_BOOTSTRAP_PASSWORD`.
- `git diff --check`: passed (exit 0; Git emitted a normal LF-to-CRLF warning).
- Secret scan (sensitive search argument redacted in this report):
  `rg -n --hidden -g '!node_modules' -g '!forguncy-server-api/bin' -g '!forguncy-server-api/obj' '<redacted-user-provided-password>' forguncy-server-api`.
  The historical invocation used the user-provided value in place of the
  placeholder and returned no matches (exit 1).
- README focused scan:
  `FGC_AUTH_BOOTSTRAP_USERNAME=True`, `FGC_AUTH_BOOTSTRAP_PASSWORD=True`,
  `FGC_BOOTSTRAP_USERNAME=False`, `FGC_BOOTSTRAP_PASSWORD=False`.
