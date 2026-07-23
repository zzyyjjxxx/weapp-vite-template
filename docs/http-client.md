# HTTP client

The public JSON entry point is `src/shared/http/client.ts`; domain code should
not call `wevu/fetch` or the transport seam directly. The default base URL is
`http://127.0.0.1:8787/api` and can be changed with `VITE_API_BASE_URL`.

## Envelope and errors

Hono returns `{ code, message, data, traceId }`. Successful codes are accepted
by `transport.ts`; non-2xx responses, business failures, decode failures,
network failures, timeout and external cancellation become `ApiError` with a
stable `kind`, optional status/code and trace ID.

## Auth

- `auth: 'none'`: login and refresh.
- `auth: 'required'`: profile and orders; missing access token fails before
  transport.
- `auth: 'optional'`: available for future public-or-private endpoints.

The client adds the bearer token, handles one 401 replay through a single-flight
refresh, and never recursively refreshes the refresh call. Refresh failure
clears the session and private Query cache.

## Cancellation and logging

Query Core's `signal` is passed through domain Service GET calls into the
transport. Timeout and external abort remain distinguishable. Logs contain
route/status/error kind/code/trace ID only; credentials, headers, bodies and
personal data are not logged.

The local endpoints are implemented in `server/`:
`/health`, `/auth/login`, `/auth/refresh`, `/profile`, `/orders`,
`/orders/:id`, and `/orders/:id/cancel`.
