# c_userinfo MD5 authentication design

## Goal

Adapt the existing Forguncy login flow to authenticate against the real
`c_userinfo` table and its existing lowercase 16-character MD5 password
values, while keeping the single external login API and JWT response contract.

## Confirmed data contract

The real user table is `c_userinfo` with these relevant columns:

| Real column | Application meaning | Login use |
| --- | --- | --- |
| `id` | User ID | Included in the JWT subject/user response |
| `creditCode` | Enterprise credit code | Login account; exposed through the existing `username` request/response field |
| `password` | Stored password | Compared with the lowercase 16-character MD5 value |
| `isopen` | Whether the account is opened | Only `isopen = 1` may log in |

`businessName`, `region`, `county`, `telephone`, and `promiseState` remain
unmapped for this login-only scope.

## Password rule

For a supplied password, encode the password as UTF-8, calculate the standard
MD5 digest, format the digest as lowercase hexadecimal, and take the middle
16 characters:

```text
lowercaseHex(MD5(UTF8(password))).Substring(8, 16)
```

There is no salt, PBKDF2 fallback, or password migration in this scope. The
stored value is compared using a fixed-time comparison.

## Application changes

- Map the existing authentication entity to `c_userinfo` using `creditCode`,
  `password`, and `isopen` instead of the old `jwt_users` columns.
- Remove `jwt_users` schema creation and bootstrap-user insertion. The API
  must not create, alter, or seed the real user table during login startup.
- Replace PBKDF2 hashing/verification with the confirmed MD5 middle-16 rule.
- Keep `POST /customapi/authapi/login`, the request fields, JWT issuance,
  validation services, and 200/400/401/500 response shapes unchanged.
- Keep the existing Forguncy SDK lookup of `config.item='ssl'` as the source
  of the database connection string.
- Update documentation and tests so they no longer describe `jwt_users` or
  bootstrap credentials as part of the real-user login path.

## Error and security boundaries

- Missing or malformed user data continues to produce the existing fixed
  invalid-credentials response; database/configuration failures continue to
  produce the fixed server-error response.
- No password, MD5 value, connection string, or database credential is logged
  or returned.
- The implementation does not perform a live database migration or create a
  local database as part of this change.

## Verification

- Unit tests cover the exact MD5 vector, lowercase/middle-16 formatting,
  `creditCode` lookup, `isopen` gating, and the mapped `c_userinfo` model.
- Release tests and the .NET 6 Release production build use the installed
  Forguncy 8.0.4 SDK path.
- Live MySQL, Forguncy designer upload, and HTTP verification remain separate
  runtime checks and are reported honestly if unavailable.
