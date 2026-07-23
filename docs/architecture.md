# Architecture

This repository is a local WeChat Mini Program scaffold with a thin Hono test
backend.

## Package boundaries

- `src/`: weapp-vite + Wevu Vue SFC application.
- `src/features/<domain>/`: typed domain models, Service calls, Query keys and
  Query/Mutation wrappers.
- `src/shared/http/`: the only JSON transport boundary. It uses `wevu/fetch`,
  maps envelopes/errors to `ApiError`, and owns one-time token refresh replay.
- `src/shared/query/`: the repository adapter from `@tanstack/query-core` to
  Wevu refs/lifecycle.
- `src/stores/`: short-lived client state: auth session, app readiness,
  preferences and versioned auth persistence.
- `src/router/`: generated physical route types, metadata, query parsing and
  typed navigation wrapper.
- `src/platform/`: host adapters such as `wpi` storage and network status.
- `server/`: Hono App, deterministic in-memory fixtures and Node entrypoint.

## Data flow

```text
Page -> domain Query/Mutation -> domain Service -> request()
     -> transportRequest() -> wevu/fetch -> local Hono App -> fixture
```

Query Core owns server state and cache lifecycle. Wevu Store owns client state;
Query data is not copied into Store. Authenticated queries use the `private`
scope and are removed on logout or refresh failure.

## Runtime choices

Pages use `<script setup lang="ts">` and import runtime APIs from `wevu`.
The app shell initializes Store plugins, typed Router, Query online manager and
focus hooks synchronously. Orders are in `subpackages/order` so the generated
`app.json` and typed router keep the boundary explicit.

`weapp-tailwindcss` remains an installed mini-program compatibility option.
This project deliberately does not add native Web TailwindCSS config, class
scanning or browser runtime; current UI styling is SCSS tokens and semantic
classes.

The design and implementation decisions are recorded in:
`docs/superpowers/specs/2026-07-23-weapp-vite-hono-scaffold-design.md` and
`docs/superpowers/plans/2026-07-23-weapp-vite-hono-scaffold.md`.
